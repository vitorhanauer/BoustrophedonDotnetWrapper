using BoustrophedonDotnetWrapper;

namespace BoustrophedonDotnetWrapper.Tests;

public class DomainValidationTests
{
    public static IEnumerable<object[]> InvalidObstacleConfigurations =>
    [
        ["obstacle touches area", new[] { Rectangle(0, 3, 2, 5) }],
        ["obstacles touch", new[] { Rectangle(2, 2, 4, 4), Rectangle(4, 2, 6, 4) }],
        ["obstacles overlap", new[] { Rectangle(2, 2, 5, 5), Rectangle(4, 4, 7, 7) }],
        ["one obstacle nests inside another", new[] { Rectangle(2, 2, 8, 8), Rectangle(3, 3, 4, 4) }]
    ];

    [Theory]
    [InlineData(-1, 5, 5, 5)]
    [InlineData(5, 5, 11, 5)]
    public void GenerateWaypoints_WhenStartOrEndIsOutsideArea_ThrowsArgumentException(
        double startX, double startY, double endX, double endY)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(startX, startY),
                new Point(endX, endY),
                Rectangle(0, 0, 10, 10),
                [],
                width: 2,
                overlapPercent: 25));
    }

    [Theory]
    [MemberData(nameof(InvalidObstacleConfigurations))]
    public void GenerateWaypoints_WhenObstaclesTouchOverlapOrNest_ThrowsArgumentException(
        string scenario, Point[][] obstacles)
    {
        _ = scenario;
        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(1, 1),
                new Point(9, 9),
                Rectangle(0, 0, 10, 10),
                obstacles,
                width: 2,
                overlapPercent: 25));
    }

    private static Point[] Rectangle(double minX, double minY, double maxX, double maxY) =>
    [
        new Point(minX, minY),
        new Point(maxX, minY),
        new Point(maxX, maxY),
        new Point(minX, maxY)
    ];
}
