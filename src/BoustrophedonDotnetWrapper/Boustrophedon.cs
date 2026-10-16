using static BoustrophedonDotnetWrapper.NativeMethods;

namespace BoustrophedonDotnetWrapper;

/// <summary>Generates coverage paths with the native Boustrophedon planner.</summary>
public static class Boustrophedon
{
    /// <summary>Returns a managed path through a Cartesian polygon and its free domain.</summary>
    /// <param name="start">First waypoint, in the free domain.</param>
    /// <param name="end">Last waypoint, in the free domain.</param>
    /// <param name="area">Vertices of a simple outer polygon.</param>
    /// <param name="obstacles">Disjoint polygons strictly inside the area; use an empty array for none.</param>
    /// <param name="width">Positive full coverage diameter, in the coordinate length unit.</param>
    /// <param name="overlapPercent">Coverage overlap in [0, 100).</param>
    /// <returns>An independently owned array starting and ending at the requested points.</returns>
    /// <remarks>Do not mutate the inputs during this synchronous call. Boundaries are traversable.</remarks>
    /// <exception cref="ArgumentNullException">An input array is null.</exception>
    /// <exception cref="ArgumentException">Arguments or geometry are invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Width or overlap is outside its valid range.</exception>
    /// <exception cref="InvalidOperationException">No path can be generated, or the native status is unknown.</exception>
    /// <exception cref="OverflowException">The geometry or path exceeds representation limits.</exception>
    /// <exception cref="OutOfMemoryException">A managed or native allocation failed.</exception>
    /// <exception cref="PlatformNotSupportedException">The process is not Linux x64.</exception>
    public static unsafe Point[] GenerateWaypoints(Point start, Point end, Point[] area,
        Point[][] obstacles, double width, double overlapPercent)
    {
        ArgumentNullException.ThrowIfNull(area);
        ArgumentNullException.ThrowIfNull(obstacles);
        if (!double.IsFinite(width) || width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be finite and positive.");
        if (!double.IsFinite(overlapPercent) || overlapPercent < 0 || overlapPercent >= 100)
            throw new ArgumentOutOfRangeException(nameof(overlapPercent), "Overlap must be finite and in [0, 100).");
        ValidatePoint(start, nameof(start));
        ValidatePoint(end, nameof(end));
        ValidatePolygon(area, nameof(area));

        int pointCount = area.Length;
        foreach (Point[] obstacle in obstacles)
        {
            if (obstacle is null)
                throw new ArgumentException("An obstacle polygon cannot be null.", nameof(obstacles));
            ValidatePolygon(obstacle, nameof(obstacles));
            pointCount = checked(pointCount + obstacle.Length);
        }
        if (pointCount > Array.MaxLength)
            throw new OverflowException("Input vertices cannot fit in a managed array.");
        if (!OperatingSystem.IsLinux() || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.X64)
            throw new PlatformNotSupportedException("This package supports Linux x64 with glibc only.");

        var points = new NativePoint[pointCount];
        var polygons = new NativePolygon[obstacles.Length];
        int offset = 0;
        foreach (Point point in area)
            points[offset++] = ToNative(point);
        foreach (Point[] obstacle in obstacles)
            foreach (Point point in obstacle)
                points[offset++] = ToNative(point);

        fixed (NativePoint* pointBuffer = points)
        fixed (NativePolygon* polygonBuffer = polygons)
        {
            offset = area.Length;
            for (int i = 0; i < obstacles.Length; i++)
            {
                polygons[i] = new NativePolygon { Vertices = pointBuffer + offset, Count = (nuint)obstacles[i].Length };
                offset += obstacles[i].Length;
            }

            NativePath path = default;
            try
            {
                int status = NativeMethods.GenerateWaypoints(ToNative(start), ToNative(end),
                    new NativePolygon { Vertices = pointBuffer, Count = (nuint)area.Length },
                    polygonBuffer, (nuint)obstacles.Length, width, overlapPercent, &path);
                ThrowForStatus(status);

                if (path.Count > (nuint)Array.MaxLength)
                    throw new OverflowException("The native path cannot fit in a managed array.");
                var result = new Point[(int)path.Count];
                for (int i = 0; i < result.Length; i++)
                    result[i] = new Point(path.Points[i].X, path.Points[i].Y);
                return result;
            }
            finally
            {
                if (path.Points != null)
                    FreeWaypoints(&path);
            }
        }
    }

    private static NativePoint ToNative(Point point) => new() { X = point.X, Y = point.Y };

    private static void ThrowForStatus(int status)
    {
        switch (status)
        {
            case 0: return;
            case 1: throw new ArgumentException("The native planner rejected an argument.");
            case 2: throw new ArgumentException("Invalid polygon geometry or start/end outside the free domain.");
            case 3: throw new InvalidOperationException("The native planner could not find a path.");
            case 4: throw new OverflowException("The native planner exceeded a representation limit.");
            case 5: throw new OutOfMemoryException("The native planner could not allocate memory.");
            default: throw new InvalidOperationException($"Unknown native planner status: {status}.");
        }
    }

    private static void ValidatePoint(Point point, string parameterName)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
            throw new ArgumentException("Coordinates must be finite.", parameterName);
    }

    private static void ValidatePolygon(Point[] vertices, string parameterName)
    {
        if (vertices.Length < 3)
            throw new ArgumentException("A polygon requires at least three vertices.", parameterName);
        foreach (Point point in vertices)
            ValidatePoint(point, parameterName);
    }
}
