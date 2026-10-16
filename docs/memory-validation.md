# .NET wrapper memory validation

`scripts/check-memory.sh [iterations]` builds a .NET 10 consumer and repeatedly calls the public API under Valgrind Memcheck inside a container limited to 1 GiB. The default is 1,000 iterations; the script accepts 1 through 10,000.

The consumer uses a 100 × 100 square with no obstacles, width 10, overlap 0%, and start/end at opposite corners. Each iteration calls `Boustrophedon.GenerateWaypoints`; the wrapper copies the route into managed memory and calls `freeWaypoints` in a `finally` block. The pinned core submodule is rebuilt with debug symbols inside the container so allocation stacks identify `libboustrophedon.so`.

The script requires a clean submodule at the pinned commit before starting Docker. Memcheck writes its full XML report to `artifacts/memory/valgrind.xml`. Validation fails on definitely, indirectly or possibly lost blocks, or on `InvalidRead`, `InvalidWrite`, `InvalidFree` and `MismatchedFree`, when the error stack contains `libboustrophedon.so`. Events without that frame are outside the core criteria, so internal .NET runtime memory alone does not cause failures. There is no assertion on RSS or peak memory usage.

## Recorded result

On 2026-10-16, `scripts/check-memory.sh 1000` completed successfully using `mcr.microsoft.com/dotnet/sdk:10.0-noble` and Valgrind 3.22.0. The original validation used core commit `edbab6978e0b33cbc9cf1b88815c19111429303b`, before the extra-contour fix. The 1,000 calls returned 26,000 waypoints in total (26 per call); the report contained no lost blocks or invalid read/write/free errors attributed to the core. The full XML report is available at `artifacts/memory/valgrind.xml` after execution.

This is a historical result for the rectangular scenario above and the API's normal success path. It does not establish the absence of leaks for inputs with obstacles, invalid areas or allocation failures, nor does it characterize memory retained by the .NET runtime. The stack filter excludes runtime errors and allocations that Valgrind cannot attribute to the core library. Rerun the command to validate the currently pinned core; waypoint counts can change between core revisions.
