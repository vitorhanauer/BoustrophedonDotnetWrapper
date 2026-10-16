using BoustrophedonDotnetWrapper;

namespace BoustrophedonDotnetWrapper.Tests;

public class ArgumentValidationTests
{
    private static readonly Point[] ValidArea =
    [
        new Point(0, 0),
        new Point(10, 0),
        new Point(10, 10),
        new Point(0, 10)
    ];

    [Fact]
    public void GenerateWaypoints_WhenAreaIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                null!,
                [],
                width: 1,
                overlapPercent: 0));

        Assert.Equal("area", exception.ParamName);
    }

    [Fact]
    public void GenerateWaypoints_WhenObstaclesIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                ValidArea,
                null!,
                width: 1,
                overlapPercent: 0));

        Assert.Equal("obstacles", exception.ParamName);
    }

    [Theory]
    [InlineData(0, 0, "width")]
    [InlineData(1, 100, "overlapPercent")]
    public void GenerateWaypoints_WhenWidthOrOverlapIsOutOfRange_ThrowsArgumentOutOfRangeException(
        double width, double overlapPercent, string parameterName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                ValidArea,
                [],
                width,
                overlapPercent));

        Assert.Equal(parameterName, exception.ParamName);
    }
}
