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
    public static unsafe Point[] GenerateWaypoints(Point start, Point end, Point[] area,
        Point[][] obstacles, double width, double overlapPercent)
    {
        int pointCount = area.Length;
        foreach (Point[] obstacle in obstacles)
            pointCount = checked(pointCount + obstacle.Length);

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
                if (status != 0)
                    throw new InvalidOperationException($"Native generation failed with status {status}.");

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
}
