#!/usr/bin/env sh
set -eu

iterations="${1:-1000}"
case "$iterations" in
    ''|*[!0-9]*)
        echo "usage: $0 [iterations from 1 to 10000]" >&2
        exit 2
        ;;
esac
if [ "$iterations" -lt 1 ] || [ "$iterations" -gt 10000 ]; then
    echo "iterations must be from 1 to 10000" >&2
    exit 2
fi

image="boustrophedon-memory-check:local"
script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
project_dir=$(CDPATH= cd -- "$script_dir/.." && pwd)
core_dir="$project_dir/native/BoustrophedonCore"
expected_core_commit="2d3af904cd785b558b9e7bd5fc9f4a7103f2042e"

if [ ! -d "$core_dir/.git" ] && [ ! -f "$core_dir/.git" ]; then
    echo "native/BoustrophedonCore submodule is missing." >&2
    exit 1
fi
actual_core_commit=$(git -C "$core_dir" rev-parse HEAD)
if [ "$actual_core_commit" != "$expected_core_commit" ]; then
    echo "Expected core commit $expected_core_commit, found $actual_core_commit." >&2
    exit 1
fi
if [ -n "$(git -C "$core_dir" status --porcelain --untracked-files=all)" ]; then
    echo "The native core worktree must be clean before memory validation." >&2
    git -C "$core_dir" status --short >&2
    exit 1
fi

result_dir="$project_dir/artifacts/memory"
mkdir -p "$result_dir"

docker build --tag "$image" --file "$project_dir/build/memory-check.Dockerfile" "$project_dir"
docker run --rm --memory=1g --memory-swap=1g \
    --user="$(id -u):$(id -g)" --env DOTNET_CLI_HOME=/tmp/dotnet-cli-home \
    --mount "type=bind,src=$result_dir,dst=/results" \
    "$image" "$iterations"
