#!/usr/bin/env bash
set -Eeuo pipefail

wrapper_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
core_dir="$wrapper_root/native/BoustrophedonCore"
expected_commit="edbab6978e0b33cbc9cf1b88815c19111429303b"
mode="host"
for arg in "$@"; do
  case "$arg" in
    --mode=host) mode=host ;;
    --mode=container) mode=container ;;
    -h|--help)
      echo "Uso: $0 [--mode=host|--mode=container]"
      exit 0
      ;;
    *) echo "Argumento desconhecido: $arg" >&2; exit 2 ;;
  esac
done

if [[ ! -d "$core_dir/.git" && ! -f "$core_dir/.git" ]]; then
  echo "Submódulo ausente: $core_dir. Inicialize native/BoustrophedonCore." >&2
  exit 1
fi
actual_commit="$(git -C "$core_dir" rev-parse HEAD)"
if [[ "$actual_commit" != "$expected_commit" ]]; then
  echo "Core incorreto: esperado $expected_commit, encontrado $actual_commit." >&2
  exit 1
fi
if [[ -n "$(git -C "$core_dir" status --porcelain --untracked-files=all)" ]]; then
  echo "O worktree do core precisa estar limpo; nenhuma fonte será compilada." >&2
  git -C "$core_dir" status --short >&2
  exit 1
fi

out_dir="$wrapper_root/artifacts/native/linux-x64"
tmp_dir="$(mktemp -d)"
cleanup() { rm -rf -- "$tmp_dir"; }
trap cleanup EXIT

# Build only the pinned Git tree. This excludes ignored files and prevents a
# dirty checkout from becoming compiler input after the checks above.
git -C "$core_dir" archive --format=tar "$expected_commit" > "$tmp_dir/core.tar"
mkdir "$tmp_dir/source"
tar -xf "$tmp_dir/core.tar" -C "$tmp_dir/source"

case "$mode" in
  host)
    if [[ "$(uname -s)" != Linux || "$(uname -m)" != x86_64 ]]; then
      echo "O modo host produz linux-x64 e exige Linux x86_64." >&2
      exit 1
    fi
    cmake -S "$tmp_dir/source" -B "$tmp_dir/cmake-build" -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=OFF
    cmake --build "$tmp_dir/cmake-build" --parallel --target boustrophedon_shared
    mkdir -p "$out_dir"
    cp -L "$tmp_dir/cmake-build/libboustrophedon.so" "$out_dir/libboustrophedon.so"
    ;;
  container)
    command -v docker >/dev/null || { echo "Docker não encontrado." >&2; exit 1; }
    cp "$wrapper_root/build/native-linux-x64.Dockerfile" "$tmp_dir/Dockerfile"
    docker build --platform linux/amd64 -t "boustrophedon-native:${expected_commit}" "$tmp_dir"
    mkdir -p "$out_dir"
    container_id="$(docker create --platform linux/amd64 "boustrophedon-native:${expected_commit}")"
    if ! docker cp "$container_id:/out/libboustrophedon.so" "$out_dir/libboustrophedon.so.tmp"; then
      docker rm "$container_id" >/dev/null
      exit 1
    fi
    docker rm "$container_id" >/dev/null
    mv "$out_dir/libboustrophedon.so.tmp" "$out_dir/libboustrophedon.so"
    ;;
esac

if [[ ! -f "$out_dir/libboustrophedon.so" || -L "$out_dir/libboustrophedon.so" ]]; then
  echo "Build não produziu o arquivo real libboustrophedon.so." >&2
  exit 1
fi
library_sha256="$(sha256sum "$out_dir/libboustrophedon.so" | cut -d ' ' -f 1)"
if [[ "$mode" == container ]]; then
  printf 'core_commit=%s\nmode=container\nbuilder_image=debian:12-slim\narchitecture=linux-x64\nlibrary_sha256=%s\n' \
    "$expected_commit" "$library_sha256" > "$out_dir/build-info.txt"
else
  printf 'core_commit=%s\nmode=host\narchitecture=linux-x64\nlibrary_sha256=%s\n' \
    "$expected_commit" "$library_sha256" > "$out_dir/build-info.txt"
fi
echo "Biblioteca pronta: $out_dir/libboustrophedon.so ($mode)."
