using BoustrophedonDotnetWrapper;

namespace BoustrophedonDotnetWrapper.Tests;

public class PathBehaviorTests
{
    [Fact]
    public void GenerateWaypoints_PreservesAreaAndObstacleInputs()
    {
        Point[] area = Rectangle(0, 0, 10, 10);
        Point[][] obstacles = [Rectangle(4, 4, 6, 6)];
        Point[] areaBefore = [.. area];
        Point[][] obstaclesBefore = obstacles.Select(obstacle => obstacle.ToArray()).ToArray();

        _ = Boustrophedon.GenerateWaypoints(
            new Point(0, 0),
            new Point(10, 10),
            area,
            obstacles,
            width: 2,
            overlapPercent: 25);

        Assert.Equal(areaBefore, area);
        Assert.Equal(obstaclesBefore.Length, obstacles.Length);
        for (int i = 0; i < obstacles.Length; i++)
            Assert.Equal(obstaclesBefore[i], obstacles[i]);
    }

    [Fact]
    public void GenerateWaypoints_ForConcaveArea_StaysInsideAndCoversKnownSamples()
    {
        Point[] area =
        [
            new Point(0, 0),
            new Point(8, 0),
            new Point(8, 3),
            new Point(5, 3),
            new Point(5, 8),
            new Point(0, 8)
        ];
        Point[] path = Boustrophedon.GenerateWaypoints(
            new Point(0, 0),
            new Point(8, 3),
            area,
            [],
            width: 2,
            overlapPercent: 25);
        Point[] samples = FreeGridSamples(area, []);

        AssertSafePath(path, area, []);
        AssertCoversSamples(path, samples, radius: 1);
    }

    [Fact]
    public void GenerateWaypoints_AroundObstacle_StaysInFreeSpaceAndCoversKnownSamples()
    {
        Point[] area = Rectangle(0, 0, 10, 10);
        Point[][] obstacles = [Rectangle(4, 4, 6, 6)];
        Point[] path = Boustrophedon.GenerateWaypoints(
            new Point(0, 0),
            new Point(10, 10),
            area,
            obstacles,
            width: 2,
            overlapPercent: 25);
        Point[] samples = FreeGridSamples(area, obstacles);

        AssertSafePath(path, area, obstacles);
        AssertCoversSamples(path, samples, radius: 1);
    }

    [Fact]
    public void GenerateWaypoints_AcceptsEitherWindingAndRepeatedClosingVertex()
    {
        Point[] counterClockwise = Rectangle(0, 0, 10, 10);
        Point[] clockwise = counterClockwise.Reverse().ToArray();
        Point[] withClosingVertex = [.. counterClockwise, counterClockwise[0]];
        Point[][] variants = [counterClockwise, clockwise, withClosingVertex];
        Point[] samples = FreeGridSamples(counterClockwise, []);

        foreach (Point[] area in variants)
        {
            Point[] path = Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                area,
                [],
                width: 2,
                overlapPercent: 25);

            Assert.Equal(new Point(0, 0), path[0]);
            Assert.Equal(new Point(10, 10), path[^1]);
            AssertSafePath(path, counterClockwise, []);
            AssertCoversSamples(path, samples, radius: 1);
        }
    }

    [Fact]
    public void GenerateWaypoints_WhenStartAndEndMatch_ReturnsClosedRequestedPath()
    {
        Point[] area = Rectangle(0, 0, 10, 10);
        var startAndEnd = new Point(5, 5);

        Point[] path = Boustrophedon.GenerateWaypoints(
            startAndEnd,
            startAndEnd,
            area,
            [],
            width: 2,
            overlapPercent: 25);

        Assert.True(path.Length >= 2);
        Assert.Equal(startAndEnd, path[0]);
        Assert.Equal(startAndEnd, path[^1]);
        AssertSafePath(path, area, []);
        AssertCoversSamples(path, FreeGridSamples(area, []), radius: 1);
    }

    [Fact]
    public void GenerateWaypoints_AfterInvalidGeometryFailure_SucceedsForValidArea()
    {
        Point[] selfIntersectingArea =
        [
            new Point(0, 0),
            new Point(10, 10),
            new Point(10, 0),
            new Point(0, 10)
        ];

        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                selfIntersectingArea,
                [],
                width: 2,
                overlapPercent: 25));

        Point[] area = Rectangle(0, 0, 10, 10);
        Point[] path = Boustrophedon.GenerateWaypoints(
            new Point(0, 0),
            new Point(10, 10),
            area,
            [],
            width: 2,
            overlapPercent: 25);

        Assert.Equal(new Point(0, 0), path[0]);
        Assert.Equal(new Point(10, 10), path[^1]);
        AssertSafePath(path, area, []);
    }

    [Fact]
    public async Task GenerateWaypoints_WhenCalledConcurrently_ReturnsIndependentValidPaths()
    {
        Point[] area = Rectangle(0, 0, 10, 10);
        Point[][] obstacles = [Rectangle(4, 4, 6, 6)];
        Task<Point[]>[] calls = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                area,
                obstacles,
                width: 2,
                overlapPercent: 25)))
            .ToArray();

        Point[][] paths = await Task.WhenAll(calls);

        Assert.Equal(8, paths.Length);
        for (int i = 0; i < paths.Length; i++)
        {
            Assert.Equal(new Point(0, 0), paths[i][0]);
            Assert.Equal(new Point(10, 10), paths[i][^1]);
            AssertSafePath(paths[i], area, obstacles);
            if (i > 0)
                Assert.NotSame(paths[0], paths[i]);
        }
    }

    [Fact]
    public void GenerateWaypoints_RepeatedCallsAreDeterministicAndReturnIndependentArrays()
    {
        Point[] area = Rectangle(0, 0, 10, 10);
        var start = new Point(0, 0);
        var end = new Point(10, 10);

        Point[] first = Boustrophedon.GenerateWaypoints(start, end, area, [], 2, 25);
        Point[] second = Boustrophedon.GenerateWaypoints(start, end, area, [], 2, 25);

        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        first[0] = new Point(-100, -100);
        Assert.Equal(start, second[0]);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(25, false)]
    [InlineData(75, false)]
    [InlineData(0, true)]
    [InlineData(25, true)]
    [InlineData(75, true)]
    public void GenerateWaypoints_KeepsBoundaryClearanceAndCoversRectangleEdges(
        double overlapPercent, bool includeObstacle)
    {
        Point[] area = Rectangle(0, 0, 10, 10);
        Point[][] obstacles = includeObstacle ? [Rectangle(4, 3, 6, 7)] : [];
        var start = new Point(1, 1);
        var end = new Point(9, 9);
        const double width = 1;
        double margin = width * (1 - overlapPercent / 100) / (2 * Math.Sqrt(2));

        Point[] path = Boustrophedon.GenerateWaypoints(
            start, end, area, obstacles, width, overlapPercent);

        Assert.Equal(start, path[0]);
        Assert.Equal(end, path[^1]);
        AssertSafePath(path, area, obstacles);
        foreach (Point point in path)
        {
            double outerDistance = Math.Min(Math.Min(point.X, 10 - point.X),
                Math.Min(point.Y, 10 - point.Y));
            Assert.True(outerDistance >= margin - 1e-9,
                $"Waypoint {point} violates outer boundary clearance {margin}.");
            foreach (Point[] obstacle in obstacles)
                Assert.True(DistanceToPath([.. obstacle, obstacle[0]], point) >= margin - 1e-9);
        }
        // Together with waypoint-to-edge distances, these check separation of
        // every obstacle edge from every route segment, including connectors.
        foreach (Point[] obstacle in obstacles)
        foreach (Point corner in obstacle)
            Assert.True(DistanceToPath(path, corner) >= margin - 1e-9);

        var samples = new List<Point>();
        for (int x = 0; x <= 100; x++)
        for (int y = 0; y <= 100; y++)
        {
            var sample = new Point(x / 10.0, y / 10.0);
            if (!obstacles.Any(obstacle => IsStrictlyInside(obstacle, sample)))
                samples.Add(sample);
        }
        AssertCoversSamples(path, samples.ToArray(), width / 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateWaypoints_FromBoundary_PreservesTerminalAndUsesShortApproach(bool onObstacle)
    {
        Point[] area = Rectangle(0, 0, 10, 10);
        Point[][] obstacles = onObstacle ? [Rectangle(4, 3, 6, 7)] : [];
        var start = onObstacle ? new Point(4, 5) : new Point(5, 0);
        var end = new Point(9, 9);
        double margin = 1 / (2 * Math.Sqrt(2));

        Point[] path = Boustrophedon.GenerateWaypoints(start, end, area, obstacles, 1, 0);

        Assert.Equal(start, path[0]);
        Assert.Equal(end, path[^1]);
        double dx = path[1].X - start.X;
        double dy = path[1].Y - start.Y;
        Assert.Equal(margin, Math.Sqrt(dx * dx + dy * dy), precision: 8);
        AssertSafePath(path, area, obstacles);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateWaypoints_WhenNominalMarginClosesDomain_ReducesMarginAndPreservesConnectivity(
        bool tinyArea)
    {
        Point[] area = tinyArea ? Rectangle(0, 0, 0.1, 0.1) :
        [
            new Point(0, 0), new Point(4, 0), new Point(4, 1.9),
            new Point(6, 1.9), new Point(6, 0), new Point(10, 0),
            new Point(10, 4), new Point(6, 4), new Point(6, 2.1),
            new Point(4, 2.1), new Point(4, 4), new Point(0, 4)
        ];
        var start = tinyArea ? new Point(0.02, 0.02) : new Point(1, 2);
        var end = tinyArea ? new Point(0.08, 0.08) : new Point(9, 2);

        Point[] path = Boustrophedon.GenerateWaypoints(start, end, area, [], 1, 0);

        Assert.Equal(start, path[0]);
        Assert.Equal(end, path[^1]);
        AssertSafePath(path, area, []);
        Assert.All(path, point => Assert.True(IsStrictlyInside(area, point)));
    }

    private static Point[] Rectangle(double minX, double minY, double maxX, double maxY) =>
    [
        new Point(minX, minY),
        new Point(maxX, minY),
        new Point(maxX, maxY),
        new Point(minX, maxY)
    ];

    private static Point[] FreeGridSamples(Point[] area, Point[][] obstacles)
    {
        double minX = area.Min(point => point.X);
        double maxX = area.Max(point => point.X);
        double minY = area.Min(point => point.Y);
        double maxY = area.Max(point => point.Y);
        var samples = new List<Point>();
        for (double x = minX + 0.5; x < maxX; x += 1)
        for (double y = minY + 0.5; y < maxY; y += 1)
        {
            var sample = new Point(x, y);
            if (!IsInsideOrOn(area, sample) || obstacles.Any(obstacle => IsStrictlyInside(obstacle, sample)))
                continue;
            samples.Add(sample);
        }

        return samples.ToArray();
    }

    private static void AssertSafePath(Point[] path, Point[] area, Point[][] obstacles)
    {
        Assert.True(path.Length >= 2);
        for (int segment = 1; segment < path.Length; segment++)
        for (int sample = 0; sample <= 64; sample++)
        {
            double t = sample / 64.0;
            var point = new Point(
                path[segment - 1].X + (path[segment].X - path[segment - 1].X) * t,
                path[segment - 1].Y + (path[segment].Y - path[segment - 1].Y) * t);

            Assert.True(IsInsideOrOn(area, point), $"Path leaves the area at ({point.X}, {point.Y}).");
            Assert.False(obstacles.Any(obstacle => IsStrictlyInside(obstacle, point)),
                $"Path enters an obstacle at ({point.X}, {point.Y}).");
        }
    }

    private static void AssertCoversSamples(Point[] path, Point[] samples, double radius)
    {
        Assert.NotEmpty(samples);
        foreach (Point sample in samples)
            Assert.True(DistanceToPath(path, sample) <= radius + 1e-9,
                $"Sample ({sample.X}, {sample.Y}) is farther than {radius} from the path.");
    }

    private static double DistanceToPath(Point[] path, Point point)
    {
        double minimumSquared = double.PositiveInfinity;
        for (int i = 1; i < path.Length; i++)
        {
            double dx = path[i].X - path[i - 1].X;
            double dy = path[i].Y - path[i - 1].Y;
            double lengthSquared = dx * dx + dy * dy;
            double projection = lengthSquared == 0
                ? 0
                : Math.Clamp(((point.X - path[i - 1].X) * dx + (point.Y - path[i - 1].Y) * dy)
                    / lengthSquared, 0, 1);
            double offsetX = point.X - (path[i - 1].X + projection * dx);
            double offsetY = point.Y - (path[i - 1].Y + projection * dy);
            minimumSquared = Math.Min(minimumSquared, offsetX * offsetX + offsetY * offsetY);
        }

        return Math.Sqrt(minimumSquared);
    }

    private static bool IsInsideOrOn(Point[] polygon, Point point)
    {
        bool inside = false;
        for (int i = 0, previous = polygon.Length - 1; i < polygon.Length; previous = i++)
        {
            Point a = polygon[previous];
            Point b = polygon[i];
            if (IsOnSegment(a, b, point))
                return true;

            if ((a.Y > point.Y) != (b.Y > point.Y)
                && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }

        return inside;
    }

    private static bool IsStrictlyInside(Point[] polygon, Point point)
    {
        if (Enumerable.Range(0, polygon.Length).Any(i =>
            IsOnSegment(polygon[i], polygon[(i + 1) % polygon.Length], point)))
            return false;
        return IsInsideOrOn(polygon, point);
    }

    private static bool IsOnSegment(Point a, Point b, Point point)
    {
        double cross = (b.X - a.X) * (point.Y - a.Y) - (b.Y - a.Y) * (point.X - a.X);
        if (Math.Abs(cross) > 1e-9)
            return false;
        return point.X >= Math.Min(a.X, b.X) - 1e-9
            && point.X <= Math.Max(a.X, b.X) + 1e-9
            && point.Y >= Math.Min(a.Y, b.Y) - 1e-9
            && point.Y <= Math.Max(a.Y, b.Y) + 1e-9;
    }
}
