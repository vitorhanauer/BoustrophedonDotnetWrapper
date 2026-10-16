# Overview

This project wraps the C implementation of the Boustrophedon algorithm in BoustrophedonCore. It exposes a high-level .NET API and is distributed as a NuGet package.

# Public API

Expose a single public operation on the `Boustrophedon` class. Conceptual signature:

```text
generateWaypoints(Start Point, End Point, Area Vertices, Obstacle Vertices, Coverage Width, Overlap): Path;
```

- **Start point:** two coordinates, such as `{x: 10, y: 10}`, where the autonomous agent starts the route.
- **End point:** two coordinates where the agent finishes the route.
- **Area vertices:** a sequence of points describing the polygon to cover, such as `[{x: 10, y: 10}, {x: 10, y: 9}, ...]`.
- **Obstacle vertices:** a sequence of polygons, each represented by its vertices, such as `[[{x: 10, y: 10}, {x: 10, y: 9}, ...], ...]`.
- **Coverage width:** the full sensing or working width, including both the left and right sides of the agent.
- **Overlap:** the percentage by which adjacent coverage swaths overlap. The API accepts `[0, 100)`.
- **Path:** the generated sequence of points, such as `[{x: 10, y: 10}, {x: 10, y: 9}, ...]`.

# Coding practices

- Use Conventional Commits and commit each completed functional unit of work.
- Use readable names for variables, functions and other identifiers.
- Use TDD: write a failing functional test before implementing behavior.
