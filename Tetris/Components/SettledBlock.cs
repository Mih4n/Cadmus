namespace Tetris.Components;

/// <summary>
/// One block that has come to rest in the well. <see cref="PreviousCell"/> is where it sat before the
/// last collapse, so presentation can slide it down instead of teleporting it; the rules only ever
/// read <see cref="Cell"/>.
/// </summary>
public sealed class SettledBlock(Cell cell, PieceKind kind)
{
    public Cell Cell { get; set; } = cell;
    public Cell PreviousCell { get; set; } = cell;
    public PieceKind Kind { get; } = kind;
}
