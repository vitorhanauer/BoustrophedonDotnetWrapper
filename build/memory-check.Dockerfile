FROM mcr.microsoft.com/dotnet/sdk:10.0-noble

RUN apt-get update \
    && apt-get install --no-install-recommends -y cmake gcc libc6-dev make valgrind python3 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

# Rebuild the pinned native core with symbols so Memcheck can attribute its
# allocation stacks to libboustrophedon.so.
RUN cmake -S native/BoustrophedonCore -B /tmp/core-build \
      -DCMAKE_BUILD_TYPE=Debug -DBUILD_TESTING=OFF \
    && cmake --build /tmp/core-build --parallel 2 --target boustrophedon_shared \
    && mkdir -p artifacts/native/linux-x64 \
    && cp -L /tmp/core-build/libboustrophedon.so artifacts/native/linux-x64/libboustrophedon.so

RUN mkdir -p /tmp/memory-consumer \
    && printf '%s\n' \
      '<Project Sdk="Microsoft.NET.Sdk">' \
      '  <PropertyGroup>' \
      '    <OutputType>Exe</OutputType>' \
      '    <TargetFramework>net10.0</TargetFramework>' \
      '    <ImplicitUsings>enable</ImplicitUsings>' \
      '    <Nullable>enable</Nullable>' \
      '  </PropertyGroup>' \
      '  <ItemGroup>' \
      '    <ProjectReference Include="/src/src/BoustrophedonDotnetWrapper/BoustrophedonDotnetWrapper.csproj" />' \
      '  </ItemGroup>' \
      '</Project>' > /tmp/memory-consumer/MemoryConsumer.csproj \
    && printf '%s\n' \
      'using BoustrophedonDotnetWrapper;' \
      '' \
      'int iterations = args.Length == 0 ? 1000 : int.Parse(args[0]);' \
      'if (iterations is < 1 or > 10000)' \
      '    throw new ArgumentOutOfRangeException(nameof(iterations), "Use 1 to 10000 iterations.");' \
      '' \
      'Point[] area = [new(0, 0), new(100, 0), new(100, 100), new(0, 100)];' \
      'Point[][] obstacles = [];' \
      'Point start = new(0, 0);' \
      'Point end = new(100, 100);' \
      'long waypointCount = 0;' \
      'for (int i = 0; i < iterations; i++)' \
      '{' \
      '    Point[] path = Boustrophedon.GenerateWaypoints(start, end, area, obstacles, 10, 0);' \
      '    waypointCount += path.Length;' \
      '}' \
      '' \
      'Console.WriteLine(string.Format("completed={0} waypoint_count={1}", iterations, waypointCount));' \
      > /tmp/memory-consumer/Program.cs

RUN dotnet build /tmp/memory-consumer/MemoryConsumer.csproj --configuration Release

RUN printf '%s\n' \
      '#!/bin/sh' \
      'set -eu' \
      'iterations="${1:-1000}"' \
      'case "$iterations" in' \
      '  ""|*[!0-9]*) echo "iterations must be an integer from 1 to 10000" >&2; exit 2 ;;' \
      'esac' \
      'if [ "$iterations" -lt 1 ] || [ "$iterations" -gt 10000 ]; then' \
      '  echo "iterations must be from 1 to 10000" >&2' \
      '  exit 2' \
      'fi' \
      'valgrind --tool=memcheck --leak-check=full --show-leak-kinds=all --num-callers=30 --xml=yes --xml-file=/results/valgrind.xml dotnet /tmp/memory-consumer/bin/Release/net10.0/MemoryConsumer.dll "$iterations"' \
      'python3 - "$iterations" <<'"'"'PY'"'"'' \
      'import sys' \
      'import xml.etree.ElementTree as ET' \
      'root = ET.parse("/results/valgrind.xml").getroot()' \
      'core_errors = []' \
      'for error in root.findall(".//error"):' \
      '    kind = error.findtext("kind", "")' \
      '    if kind not in {"Leak_DefinitelyLost", "Leak_IndirectlyLost", "Leak_PossiblyLost", "InvalidRead", "InvalidWrite", "InvalidFree", "MismatchedFree"}:' \
      '        continue' \
      '    frames = error.findall("./stack/frame")' \
      '    if not any("libboustrophedon.so" in (frame.findtext("obj", "") or "") for frame in frames):' \
      '        continue' \
      '    detail = error.findtext("xwhat/text") or error.findtext("what") or kind' \
      '    core_errors.append((kind, detail))' \
      'print("Valgrind examined {} wrapper calls.".format(sys.argv[1]))' \
      'if core_errors:' \
      '    for kind, detail in core_errors:' \
      '        print("CORE {}: {}".format(kind, detail))' \
      '    raise SystemExit(1)' \
      'print("No lost blocks or invalid read/write/free errors had a stack in libboustrophedon.so.")' \
      'PY' > /usr/local/bin/check-memory \
    && chmod +x /usr/local/bin/check-memory

ENTRYPOINT ["/usr/local/bin/check-memory"]
