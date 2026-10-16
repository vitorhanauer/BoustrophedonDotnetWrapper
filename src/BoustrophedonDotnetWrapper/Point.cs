namespace BoustrophedonDotnetWrapper;

/// <summary>A Cartesian point in the same length unit as the coverage width.</summary>
/// <param name="X">Horizontal coordinate.</param>
/// <param name="Y">Vertical coordinate.</param>
public readonly record struct Point(double X, double Y);
