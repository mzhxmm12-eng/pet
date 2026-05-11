using System.Runtime.InteropServices;
namespace DeskPet.App.Platform;

public static class CursorTracker
{
    public static System.Windows.Point GetScreenPosition()
    {
        return GetCursorPos(out var point)
            ? new System.Windows.Point(point.X, point.Y)
            : new System.Windows.Point(double.NaN, double.NaN);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public readonly int X;
        public readonly int Y;
    }
}
