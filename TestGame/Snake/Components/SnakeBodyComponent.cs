using Cadmus.Engine.Components;

namespace TestGame.Snake.Components;

/// <summary>
/// The snake's occupied cells and heading. Data only — moving it is
/// <see cref="Systems.SnakeMovementSystem"/>'s job.
/// </summary>
public sealed class SnakeBodyComponent : Component
{
    /// <summary>Head first, tail last.</summary>
    public List<Cell> Cells { get; } = [];

    /// <summary>Direction committed on the last tick.</summary>
    public Cell Heading { get; set; } = Cell.Right;

    /// <summary>Bumped whenever the cells change, so presentation can skip untouched frames.</summary>
    public int Revision { get; set; }

    public Cell Head => Cells[0];

    public bool Occupies(Cell cell, bool ignoreTail = false)
    {
        var count = ignoreTail ? Cells.Count - 1 : Cells.Count;

        for (int i = 0; i < count; i++)
        {
            if (Cells[i] == cell)
            {
                return true;
            }
        }

        return false;
    }
}
