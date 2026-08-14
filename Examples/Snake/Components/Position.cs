using System.Runtime.InteropServices;

namespace Snake.Components;

[StructLayout(LayoutKind.Sequential)]
public struct Position(int x, int y)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
}
