#!/usr/bin/env bash
set -Eeuo pipefail

if [[ $# -ne 1 || "$1" == "-h" || "$1" == "--help" ]]; then
  echo "Uso: $0 /caminho/para/pacote.nupkg" >&2
  exit 2
fi
package="$(realpath -m -- "$1")"
if [[ ! -f "$package" ]]; then
  echo "Pacote NuGet não encontrado: $package" >&2
  exit 1
fi
command -v dotnet >/dev/null || { echo "dotnet não encontrado." >&2; exit 1; }
command -v unzip >/dev/null || { echo "unzip não encontrado." >&2; exit 1; }

tmp_dir="$(mktemp -d)"
cleanup() { rm -rf -- "$tmp_dir"; }
trap cleanup EXIT
mkdir -p "$tmp_dir/feed" "$tmp_dir/consumer"
cp "$package" "$tmp_dir/feed/"
unzip -q "$package" -d "$tmp_dir/unpacked"
nuspec="$(find "$tmp_dir/unpacked" -maxdepth 1 -name '*.nuspec' -print -quit)"
if [[ -z "$nuspec" ]]; then
  echo "O pacote não contém um nuspec." >&2
  exit 1
fi
readarray -t package_identity < <(python3 - "$nuspec" <<'PY'
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
metadata = next(node for node in root.iter() if node.tag.rsplit('}', 1)[-1] == 'metadata')
for key in ('id', 'version'):
    node = next((child for child in metadata if child.tag.rsplit('}', 1)[-1] == key), None)
    if node is None or not node.text:
        raise SystemExit(f"nuspec sem {key}")
    print(node.text.strip())
PY
)
package_id="${package_identity[0]}"
package_version="${package_identity[1]}"

cat > "$tmp_dir/consumer/Consumer.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$package_id" Version="$package_version" />
  </ItemGroup>
</Project>
EOF

# Map the wrapper exclusively to the local feed; nuget.org supplies only
# Microsoft runtime packs when the host SDK needs them.
cat > "$tmp_dir/consumer/NuGet.Config" <<EOF
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$tmp_dir/feed" />
    <add key="runtime-packs" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local"><package pattern="$package_id" /></packageSource>
    <packageSource key="runtime-packs"><package pattern="Microsoft.*" /></packageSource>
  </packageSourceMapping>
</configuration>
EOF
export NUGET_PACKAGES="$tmp_dir/nuget-cache"
unset LD_LIBRARY_PATH
dotnet restore "$tmp_dir/consumer/Consumer.csproj" --runtime linux-x64 \
  --configfile "$tmp_dir/consumer/NuGet.Config" \
  -p:SelfContained=false -p:NuGetAudit=false
cat > "$tmp_dir/consumer/Program.cs" <<'CS'
using BoustrophedonDotnetWrapper;

Point[] path = Boustrophedon.GenerateWaypoints(
    new Point(0, 0), new Point(5, 5),
    [new Point(0, 0), new Point(5, 0), new Point(5, 5), new Point(0, 5)],
    [], 1, 0);
if (path.Length < 2 || path[0] != new Point(0, 0) || path[^1] != new Point(5, 5))
    throw new InvalidOperationException("O caminho não preservou os pontos inicial e final.");
if (path.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
    throw new InvalidOperationException("O caminho contém coordenadas não finitas.");
CS

env -u LD_LIBRARY_PATH dotnet run --project "$tmp_dir/consumer/Consumer.csproj" --no-restore
dotnet publish "$tmp_dir/consumer/Consumer.csproj" --configuration Release --runtime linux-x64 --self-contained false --no-restore --output "$tmp_dir/publish"
env -u LD_LIBRARY_PATH dotnet "$tmp_dir/publish/Consumer.dll"
echo "Pacote $package_id $package_version restaurado, executado e publicado em linux-x64 com cache isolado."
