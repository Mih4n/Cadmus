using Cadmus.Engine.Components;

namespace Tetris.Components;

/// <summary>
/// What has already settled. The blocks are the truth — presentation walks them — and the occupancy
/// grid beside them is only a lookup, kept in step by the mutations below.
/// </summary>
public sealed class WellComponent : Component
{
    private readonly int columns;
    private readonly int rows;
    private readonly bool[] occupied;

    public WellComponent(int columns, int rows)
    {
        this.columns = columns;
        this.rows = rows;
        occupied = new bool[columns * rows];
    }

    /// <summary>Settled blocks, in no particular order.</summary>
    public List<SettledBlock> Blocks { get; } = [];

    /// <summary>Bumped whenever the blocks change, so presentation can skip untouched frames.</summary>
    public int Revision { get; set; }

    /// <summary>
    /// Whether a piece may occupy this cell. Above the well is free — a rotation is allowed to kick a
    /// piece up out of the field — while the sides and the floor are not.
    /// </summary>
    public bool IsFree(Cell cell)
    {
        if (cell.X < 0 || cell.X >= columns || cell.Y >= rows)
        {
            return false;
        }

        return cell.Y < 0 || !occupied[Index(cell)];
    }

    public void Add(Cell cell, PieceKind kind)
    {
        Blocks.Add(new SettledBlock(cell, kind));

        // A block locked above the field is drawn but cannot be collided with; the next spawn failing
        // is what ends the match.
        if (cell.Y >= 0)
        {
            occupied[Index(cell)] = true;
        }

        Revision++;
    }

    /// <summary>Rows where every cell is filled, top to bottom.</summary>
    public void CollectFullRows(List<int> into)
    {
        into.Clear();

        for (int y = 0; y < rows; y++)
        {
            var full = true;

            for (int x = 0; x < columns && full; x++)
            {
                full = occupied[y * columns + x];
            }

            if (full)
            {
                into.Add(y);
            }
        }
    }

    /// <summary>
    /// Drops everything above the given rows, remembering where each block came from so the slide can
    /// be drawn. Call <see cref="Settle"/> once that animation has played out.
    /// </summary>
    public void ClearRows(IReadOnlyList<int> rows)
    {
        Blocks.RemoveAll(block => Contains(rows, block.Cell.Y));

        foreach (var block in Blocks)
        {
            var fallen = 0;

            foreach (var row in rows)
            {
                if (row > block.Cell.Y)
                {
                    fallen++;
                }
            }

            block.PreviousCell = block.Cell;
            block.Cell = block.Cell with { Y = block.Cell.Y + fallen };
        }

        RebuildOccupancy();
        Revision++;
    }

    /// <summary>Makes the current positions the resting ones, ending any interpolation.</summary>
    public void Settle()
    {
        foreach (var block in Blocks)
        {
            block.PreviousCell = block.Cell;
        }
    }

    public void Clear()
    {
        Blocks.Clear();
        Array.Clear(occupied);
        Revision++;
    }

    private void RebuildOccupancy()
    {
        Array.Clear(occupied);

        foreach (var block in Blocks)
        {
            if (block.Cell.Y >= 0)
            {
                occupied[Index(block.Cell)] = true;
            }
        }
    }

    private int Index(Cell cell) => cell.Y * columns + cell.X;

    private static bool Contains(IReadOnlyList<int> rows, int row)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] == row)
            {
                return true;
            }
        }

        return false;
    }
}
