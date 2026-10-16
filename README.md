# BoustrophedonDotnetWrapper

A .NET 10 wrapper for generating coverage waypoints with BoustrophedonCore. The public API is `BoustrophedonDotnetWrapper.Boustrophedon.GenerateWaypoints(Point start, Point end, Point[] area, Point[][] obstacles, double width, double overlapPercent)`.

`overlapPercent` must be in `[0, 100)`; 100 is rejected because it eliminates the spacing between lanes. `width` must be positive and represents the full coverage width. The returned route includes both `start` and `end`.

`area` must describe a simple polygon with at least three vertices. `obstacles` may be empty; otherwise, each obstacle must be a polygon strictly inside the area and disjoint from every other obstacle. Coordinates and widths must use the same Cartesian units. Invalid inputs, native failures and allocation errors are reported as .NET exceptions.

## Use in a .NET project

The package supports .NET 10 on Linux x64 with glibc, using Debian 12 as the build baseline. Alpine/musl, Windows, macOS and ARM64 are not included. Consumers need the .NET 10 runtime and the glibc/libm system libraries; they do not need CMake or Docker.

Install the local package in the consuming project:

```sh
dotnet add package BoustrophedonDotnetWrapper --version 0.1.3 --source /absolute/path/to/artifacts/packages
```

Call the API using `Point`, an immutable value type with `double` coordinates:

```csharp
using BoustrophedonDotnetWrapper;

Point[] route = Boustrophedon.GenerateWaypoints(
    new Point(0, 0), new Point(10, 10),
    [new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10)],
    [], width: 2, overlapPercent: 10);
```

## Prerequisites

- .NET SDK 10.
- Git and CMake for local native builds.
- Docker for building the distributable native library on Debian 12.
- The `native/BoustrophedonCore` submodule at the commit pinned by this repository and its build scripts.

Initialize the submodule after cloning the wrapper:

```sh
git submodule update --init --recursive
```

The build script checks the pinned commit and requires a clean core worktree. It uses `git archive` to create a temporary source tree from that commit and builds only those files. Local core modifications are not included.

## Build the native library

For development, use the host toolchain:

```sh
./scripts/build-native.sh --mode=host
```

For distribution, use the Debian 12 x64 baseline:

```sh
./scripts/build-native.sh --mode=container
```

Both commands produce the actual library file at `artifacts/native/linux-x64/libboustrophedon.so` and a `build-info.txt` file containing its SHA-256 hash. Container metadata records the core commit and the `debian:12-slim` image. `scripts/pack.sh` always rebuilds the library in a container before creating the Release package. `scripts/check-native.sh` verifies this provenance and rejects host builds for distribution.

## Create a NuGet package

```sh
./scripts/pack.sh
```

The script builds the Release project and writes packages to `artifacts/packages/`. The native library is included at `runtimes/linux-x64/native/libboustrophedon.so`; its provenance and hash are recorded in `build-info/build-info.txt`. The package also includes this README, the MIT `LICENSE` file and the memory validation document. NuGet license metadata points to the included license file.

Native library resolution works without `LD_LIBRARY_PATH`. Direct `dotnet pack` calls also check binary provenance and reject development artifacts built on the host.

Increase `<Version>` in `src/BoustrophedonDotnetWrapper/BoustrophedonDotnetWrapper.csproj` before distributing an updated package. Use a new version to prevent consumers from reusing a cached package. When updating the core, commit its changes, update the submodule and the pinned commit in the build/check scripts, then rebuild and validate the package.

## Verify a NuGet package

```sh
./scripts/verify-package.sh artifacts/packages/BoustrophedonDotnetWrapper.0.1.3.nupkg
```

The script creates a temporary consumer that references only the package, restores it into an isolated NuGet cache, calls the public API, then publishes and executes the consumer for `linux-x64`. The wrapper is restored exclusively from the local feed. Nuget.org supplies only Microsoft runtime/targeting packages when required by the host SDK. Execution removes `LD_LIBRARY_PATH`.

## Tests

```sh
./scripts/build-native.sh --mode=host
dotnet test
```

The tests exercise the public API with the real core. To run the C test suite:

```sh
cmake -S native/BoustrophedonCore -B artifacts/core-tests -DCMAKE_BUILD_TYPE=Release
cmake --build artifacts/core-tests --parallel 2
ctest --test-dir artifacts/core-tests --output-on-failure
```

Memory validation runs an API consumer under Valgrind in a separate container:

```sh
./scripts/check-memory.sh 1000
```

The report is written to `artifacts/memory/valgrind.xml`. See the [validation results and limitations](docs/memory-validation.md).

## Contract and failures

The method is synchronous. Input arrays are preserved; do not modify them during a call. Each result is an independent managed array, and the wrapper frees all native result memory. Independent calls may run in parallel.

| Condition | Exception |
|---|---|
| Null outer array | `ArgumentNullException` |
| Out-of-range or nonfinite width or overlap | `ArgumentOutOfRangeException` |
| Invalid coordinates, polygons or geometry | `ArgumentException` |
| No route found or unknown native status | `InvalidOperationException` |
| Representation limit, including spacing underflow | `OverflowException` |
| Managed or native allocation failure | `OutOfMemoryException` |
| Process outside Linux x64 | `PlatformNotSupportedException` |

Library loading failures retain their .NET interop exceptions. The package targets glibc; the operating system and architecture check does not detect musl in advance.

## Geometry and limitations

Rings accept either winding and a repeated closing vertex. Self-intersections, consecutive duplicate vertices, and touching, overlapping or nested obstacles are rejected. Start and end must lie in the free domain; boundary travel is allowed.

Coverage uses a disk of diameter `width`, with lane spacing `width * (1 - overlapPercent / 100)`. The sweep is vertical, with no angle search. After sweeping a cell, the route connects to the next cell or the destination without adding a complete boundary lap. Full disk coverage is verified for rectangular cells; lane spacing alone does not guarantee complete coverage near arbitrary inclined or sharp boundaries.

The algorithm is deterministic and heuristic, with no global minimum guarantee; connectors may revisit cells. Geographic conversion, physical agent radius, minimum turning radius and cancellation are not provided.

The core uses floating-point geometry. Large coordinates relative to feature sizes may require recentering or rescaling. Many vertices or very small lane spacing can require substantial CPU time and memory; there is no runtime guarantee for arbitrary inputs.

## Example

The [examples/ConsoleExample](examples/ConsoleExample) project demonstrates a direct API call. After building the native library, run:

```sh
dotnet run --project examples/ConsoleExample/ConsoleExample.csproj
```

## License

Distributed under the [MIT License](LICENSE).
