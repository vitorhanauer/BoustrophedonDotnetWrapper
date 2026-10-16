using BoustrophedonDotnetWrapper;

namespace BoustrophedonDotnetWrapper.Tests;

public class RectangleTests
{
    [Fact]
    public void GenerateWaypoints_ForValidRectangle_StartsAndEndsAtRequestedPoints()
    {
        var start = new Point(0, 0);
        var end = new Point(10, 10);
        Point[] area =
        [
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10)
        ];

        Point[] waypoints = Boustrophedon.GenerateWaypoints(
            start,
            end,
            area,
            [],
            width: 1.5,
            overlapPercent: 0);

        Assert.True(waypoints.Length >= 2);
        Assert.Equal(start, waypoints[0]);
        Assert.Equal(end, waypoints[^1]);
    }
}
