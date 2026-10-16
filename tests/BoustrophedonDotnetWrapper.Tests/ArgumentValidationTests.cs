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

    [Fact]
    public void GenerateWaypoints_WhenObstacleEntryIsNull_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                ValidArea,
                [null!],
                width: 1,
                overlapPercent: 0));
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void GenerateWaypoints_WhenStartHasNonFiniteCoordinate_ThrowsArgumentException(
        double x, double y)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(x, y),
                new Point(10, 10),
                ValidArea,
                [],
                width: 1,
                overlapPercent: 0));
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void GenerateWaypoints_WhenEndHasNonFiniteCoordinate_ThrowsArgumentException(
        double x, double y)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(x, y),
                ValidArea,
                [],
                width: 1,
                overlapPercent: 0));
    }

    [Fact]
    public void GenerateWaypoints_WhenAreaHasNonFiniteCoordinate_ThrowsArgumentException()
    {
        Point[] area = [.. ValidArea];
        area[1] = new Point(double.NaN, 0);

        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                area,
                [],
                width: 1,
                overlapPercent: 0));
    }

    [Fact]
    public void GenerateWaypoints_WhenObstacleHasNonFiniteCoordinate_ThrowsArgumentException()
    {
        Point[] obstacle =
        [
            new Point(2, 2),
            new Point(4, 2),
            new Point(4, double.NegativeInfinity),
            new Point(2, 4)
        ];

        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                ValidArea,
                [obstacle],
                width: 1,
                overlapPercent: 0));
    }

    [Fact]
    public void GenerateWaypoints_WhenAreaHasFewerThanThreeVertices_ThrowsArgumentException()
    {
        Point[] area = [new Point(0, 0), new Point(10, 0)];

        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                area,
                [],
                width: 1,
                overlapPercent: 0));
    }

    [Fact]
    public void GenerateWaypoints_WhenObstacleHasFewerThanThreeVertices_ThrowsArgumentException()
    {
        Point[] obstacle = [new Point(2, 2), new Point(4, 2)];

        Assert.ThrowsAny<ArgumentException>(() =>
            Boustrophedon.GenerateWaypoints(
                new Point(0, 0),
                new Point(10, 10),
                ValidArea,
                [obstacle],
                width: 1,
                overlapPercent: 0));
    }

    [Theory]
    [InlineData(0, 0, "width")]
    [InlineData(-1, 0, "width")]
    [InlineData(double.NaN, 0, "width")]
    [InlineData(double.PositiveInfinity, 0, "width")]
    [InlineData(1, 100, "overlapPercent")]
    [InlineData(1, -1, "overlapPercent")]
    [InlineData(1, double.NaN, "overlapPercent")]
    [InlineData(1, double.PositiveInfinity, "overlapPercent")]
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
