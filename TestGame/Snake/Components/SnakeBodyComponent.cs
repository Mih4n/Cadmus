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

    /// <summary>
    /// Where each segment sat before the last move. Presentation interpolates between this and
    /// <see cref="Cells"/> so the snake glides rather than jumps; the rules never read it.
    /// </summary>
    public List<Cell> PreviousCells { get; } = [];

    /// <summary>Direction committed on the last tick.</summary>
    public Cell Heading { get; set; } = Cell.Right;

    /// <summary>Bumped whenever the cells change, so presentation can skip untouched frames.</summary>
    public int Revision { get; set; }

    public Cell Head => Cells[0];

    /// <summary>Snapshots the current cells as the origin of the next move.</summary>
    public void RecordPrevious()
    {
        PreviousCells.Clear();
        PreviousCells.AddRange(Cells);
    }

    /// <summary>Where segment <paramref name="index"/> started the current move.</summary>
    public Cell PreviousOf(int index)
    {
        if (PreviousCells.Count == 0)
        {
            return Cells[index];
        }

        // A segment added by growth has no previous position: it grows out of the old tail.
        return index < PreviousCells.Count ? PreviousCells[index] : PreviousCells[^1];
    }

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
