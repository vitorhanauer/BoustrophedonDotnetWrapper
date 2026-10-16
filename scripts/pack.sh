#!/usr/bin/env bash
set -Eeuo pipefail

wrapper_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
project="$wrapper_root/src/BoustrophedonDotnetWrapper/BoustrophedonDotnetWrapper.csproj"
expected_commit="856378566266e83bd16314aee9a902ca5e3a8662"
core_dir="$wrapper_root/native/BoustrophedonCore"
packages_dir="$wrapper_root/artifacts/packages"

if [[ ! -f "$project" ]]; then
  echo "Projeto .NET não encontrado: $project" >&2
  exit 1
fi
if [[ ! -d "$core_dir/.git" && ! -f "$core_dir/.git" ]]; then
  echo "Submódulo ausente: $core_dir." >&2
  exit 1
fi
actual_commit="$(git -C "$core_dir" rev-parse HEAD)"
if [[ "$actual_commit" != "$expected_commit" ]]; then
  echo "Core incorreto: esperado $expected_commit, encontrado $actual_commit." >&2
  exit 1
fi
if [[ -n "$(git -C "$core_dir" status --porcelain --untracked-files=all)" ]]; then
  echo "O worktree do core precisa estar limpo para empacotar." >&2
  git -C "$core_dir" status --short >&2
  exit 1
fi

# Always rebuild this artifact in the pinned Debian 12 baseline before pack.
"$wrapper_root/scripts/build-native.sh" --mode=container
"$wrapper_root/scripts/check-native.sh"

mkdir -p "$packages_dir"
dotnet build "$project" --configuration Release
dotnet pack "$project" --configuration Release --no-build --output "$packages_dir"
