namespace TestGame.Snake;

/// <summary>
/// A position on the play grid. Y grows downwards, matching the engine's pixel-space camera, so
/// <see cref="Up"/> is negative on Y.
/// </summary>
public readonly record struct Cell(int X, int Y)
{
    public static readonly Cell Up = new(0, -1);
    public static readonly Cell Down = new(0, 1);
    public static readonly Cell Left = new(-1, 0);
    public static readonly Cell Right = new(1, 0);

    public static Cell operator +(Cell left, Cell right) => new(left.X + right.X, left.Y + right.Y);

    public static Cell operator -(Cell cell) => new(-cell.X, -cell.Y);

    public bool IsOpposite(Cell other) => X == -other.X && Y == -other.Y;
}
