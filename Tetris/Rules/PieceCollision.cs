using Tetris.Components;

namespace Tetris.Rules;

/// <summary>
/// The one place that answers "does this piece fit there". Movement, rotation, the landing preview and
/// the spawn check all go through it, so the well's shape rules are stated exactly once and none of
/// them knows a thing about pixels.
/// </summary>
public static class PieceCollision
{
    public static bool Fits(WellComponent well, PieceKind kind, int rotation, Cell origin)
    {
        foreach (var offset in Tetromino.Cells(kind, rotation))
        {
            if (!well.IsFree(origin + offset))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The lowest place the piece can reach from where it is, which is also the ghost.</summary>
    public static Cell Landing(WellComponent well, PieceKind kind, int rotation, Cell origin)
    {
        var cell = origin;

        while (Fits(well, kind, rotation, cell + Cell.Down))
        {
            cell += Cell.Down;
        }

        return cell;
    }

    /// <summary>
    /// Turns the piece, trying the kick offsets in order until one fits. False leaves the piece
    /// untouched — a rotation that cannot be placed simply does not happen.
    /// </summary>
    public static bool TryRotate(
        WellComponent well,
        PieceKind kind,
        int rotation,
        Cell origin,
        bool clockwise,
        out int rotated,
        out Cell kicked)
    {
        rotated = Tetromino.Rotate(rotation, clockwise);

        foreach (var kick in Tetromino.Kicks(kind, rotation, clockwise))
        {
            kicked = origin + kick;

            if (Fits(well, kind, rotated, kicked))
            {
                return true;
            }
        }

        rotated = rotation;
        kicked = origin;

        return false;
    }
}
