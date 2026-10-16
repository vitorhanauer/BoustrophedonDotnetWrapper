using BoustrophedonDotnetWrapper;

var start = new Point(0, 0);
var end = new Point(10, 10);
Point[] area =
[
    new Point(0, 0),
    new Point(10, 0),
    new Point(10, 10),
    new Point(0, 10)
];
Point[][] obstacles = [];

var waypoints = Boustrophedon.GenerateWaypoints(
    start,
    end,
    area,
    obstacles,
    width: 2,
    overlapPercent: 10);

foreach (var point in waypoints)
    Console.WriteLine($"({point.X}, {point.Y})");
