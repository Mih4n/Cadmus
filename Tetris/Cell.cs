namespace Tetris;

/// <summary>
/// A position on the well grid. Y grows downwards, matching the engine's pixel-space camera, so a
/// piece falls towards positive Y.
/// </summary>
public readonly record struct Cell(int X, int Y)
{
    public static readonly Cell Down = new(0, 1);
    public static readonly Cell Left = new(-1, 0);
    public static readonly Cell Right = new(1, 0);

    public static Cell operator +(Cell left, Cell right) => new(left.X + right.X, left.Y + right.Y);

    public static Cell operator -(Cell left, Cell right) => new(left.X - right.X, left.Y - right.Y);
}
