using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BoustrophedonDotnetWrapper;

internal static unsafe partial class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        internal double X;
        internal double Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePolygon
    {
        internal NativePoint* Vertices;
        internal nuint Count;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePath
    {
        internal NativePoint* Points;
        internal nuint Count;
    }

    [LibraryImport("boustrophedon", EntryPoint = "generateWaypoints")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int GenerateWaypoints(NativePoint start, NativePoint end,
        NativePolygon area, NativePolygon* obstacles, nuint obstacleCount,
        double width, double overlapPercent, NativePath* output);

    [LibraryImport("boustrophedon", EntryPoint = "freeWaypoints")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void FreeWaypoints(NativePath* path);
}
