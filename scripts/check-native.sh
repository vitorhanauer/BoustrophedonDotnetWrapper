#!/usr/bin/env bash
set -Eeuo pipefail

wrapper_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
core_dir="$wrapper_root/native/BoustrophedonCore"
expected_commit="d27867f1f60ffe8af7c5c0ba20f85cf88dc67933"
artifact_dir="$wrapper_root/artifacts/native/linux-x64"
library="$artifact_dir/libboustrophedon.so"
info="$artifact_dir/build-info.txt"

if [[ ! -d "$core_dir/.git" && ! -f "$core_dir/.git" ]]; then
  echo "Submódulo ausente: $core_dir." >&2
  exit 1
fi
actual_commit="$(git -C "$core_dir" rev-parse HEAD)"
[[ "$actual_commit" == "$expected_commit" ]] || {
  echo "Core incorreto: esperado $expected_commit, encontrado $actual_commit." >&2
  exit 1
}
if [[ -n "$(git -C "$core_dir" status --porcelain --untracked-files=all)" ]]; then
  echo "O worktree do core precisa estar limpo." >&2
  git -C "$core_dir" status --short >&2
  exit 1
fi
[[ -f "$library" && ! -L "$library" ]] || {
  echo "Biblioteca nativa ausente ou não é arquivo real: $library" >&2
  exit 1
}
[[ -f "$info" ]] || { echo "Metadata de build ausente: $info" >&2; exit 1; }
grep -Fxq "core_commit=$expected_commit" "$info"
grep -Fxq 'mode=container' "$info"
grep -Fxq 'builder_image=debian:12-slim' "$info"
grep -Fxq 'architecture=linux-x64' "$info"
expected_sha256="$(sed -n 's/^library_sha256=//p' "$info")"
if [[ ! "$expected_sha256" =~ ^[0-9a-f]{64}$ ]]; then
  echo "SHA-256 inválido ou ausente em $info." >&2
  exit 1
fi
actual_sha256="$(sha256sum "$library" | cut -d ' ' -f 1)"
[[ "$actual_sha256" == "$expected_sha256" ]] || {
  echo "SHA-256 da biblioteca não corresponde à metadata do build." >&2
  exit 1
}
