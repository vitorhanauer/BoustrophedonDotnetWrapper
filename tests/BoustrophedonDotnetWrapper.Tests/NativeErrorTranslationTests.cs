using BoustrophedonDotnetWrapper;

namespace BoustrophedonDotnetWrapper.Tests;

public class NativeErrorTranslationTests
{
    [Fact]
    public void GenerateWaypoints_WhenAreaSelfIntersects_ThrowsArgumentException()
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
                width: 1,
                overlapPercent: 0));
    }

    [Fact]
    public void GenerateWaypoints_WhenLaneSpacingUnderflows_ThrowsOverflowException()
    {
        Point[] area =
        [
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10)
        ];

        Assert.Throws<OverflowException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                area,
                [],
                width: double.Epsilon,
                overlapPercent: 99));
    }
}
