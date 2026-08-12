namespace Tetris;

/// <summary>
/// Shape data for the seven tetrominoes: which cells each rotation state occupies inside the piece's
/// own bounding box, and the offsets a rotation may be nudged by when it does not fit.
/// </summary>
/// <remarks>
/// Rotation is the Super Rotation System: every state is the previous one turned inside a fixed box
/// — 3x3 for J, L, S, T and Z, 4x4 for I, 2x2 for O — which is why the box size travels with the
/// shape. The kick offsets are the published SRS tables with Y negated, because the grid here grows
/// downwards.
/// </remarks>
public static class Tetromino
{
    public const int RotationCount = 4;

    /// <summary>Cells every piece is made of. Handy for sizing sprite sets up front.</summary>
    public const int BlockCount = 4;

    private static readonly Dictionary<PieceKind, Shape> Shapes = BuildShapes();

    /// <summary>Width and height of the box the piece rotates inside, in cells.</summary>
    public static int BoxSize(PieceKind kind) => Shapes[kind].BoxSize;

    /// <summary>
    /// Point the piece turns around, in cell units from the box origin — the box centre, which is
    /// what SRS rotates about.
    /// </summary>
    public static float Pivot(PieceKind kind) => Shapes[kind].BoxSize / 2f;

    /// <summary>Cells of a rotation state, relative to the box origin. Order is stable across states.</summary>
    public static Cell[] Cells(PieceKind kind, int rotation) => Shapes[kind].States[Wrap(rotation)];

    /// <summary>Topmost row the spawn state occupies, so a piece can be placed fully inside the well.</summary>
    public static int TopRow(PieceKind kind) => Shapes[kind].TopRow;

    public static int Rotate(int rotation, bool clockwise) => Wrap(rotation + (clockwise ? 1 : 3));

    /// <summary>
    /// Offsets to try, in order, when rotating out of <paramref name="from"/>. The first entry is
    /// always no offset: a rotation that already fits is never kicked.
    /// </summary>
    public static Cell[] Kicks(PieceKind kind, int from, bool clockwise) => kind switch
    {
        // A square rotates onto itself, so it can never need a kick.
        PieceKind.O => NoKicks,
        PieceKind.I => clockwise ? LineKicksClockwise[Wrap(from)] : LineKicksCounterClockwise[Wrap(from)],
        _ => clockwise ? KicksClockwise[Wrap(from)] : KicksCounterClockwise[Wrap(from)]
    };

    private static int Wrap(int rotation) => ((rotation % RotationCount) + RotationCount) % RotationCount;

    private static Dictionary<PieceKind, Shape> BuildShapes() => new()
    {
        [PieceKind.I] = Shape.FromSpawnState(4, [new(0, 1), new(1, 1), new(2, 1), new(3, 1)]),
        [PieceKind.J] = Shape.FromSpawnState(3, [new(0, 0), new(0, 1), new(1, 1), new(2, 1)]),
        [PieceKind.L] = Shape.FromSpawnState(3, [new(2, 0), new(0, 1), new(1, 1), new(2, 1)]),
        [PieceKind.O] = Shape.FromSpawnState(2, [new(0, 0), new(1, 0), new(0, 1), new(1, 1)]),
        [PieceKind.S] = Shape.FromSpawnState(3, [new(1, 0), new(2, 0), new(0, 1), new(1, 1)]),
        [PieceKind.T] = Shape.FromSpawnState(3, [new(1, 0), new(0, 1), new(1, 1), new(2, 1)]),
        [PieceKind.Z] = Shape.FromSpawnState(3, [new(0, 0), new(1, 0), new(1, 1), new(2, 1)])
    };

    private static readonly Cell[] NoKicks = [new(0, 0)];

    // Indexed by the state being rotated out of: 0 = spawn, 1 = right, 2 = upside down, 3 = left.
    private static readonly Cell[][] KicksClockwise =
    [
        [new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2)],
        [new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2)],
        [new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2)],
        [new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2)]
    ];

    private static readonly Cell[][] KicksCounterClockwise =
    [
        [new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2)],
        [new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2)],
        [new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2)],
        [new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2)]
    ];

    // The line piece pivots differently and needs its own table, or a spin against a wall fails.
    private static readonly Cell[][] LineKicksClockwise =
    [
        [new(0, 0), new(-2, 0), new(1, 0), new(-2, 1), new(1, -2)],
        [new(0, 0), new(-1, 0), new(2, 0), new(-1, -2), new(2, 1)],
        [new(0, 0), new(2, 0), new(-1, 0), new(2, -1), new(-1, 2)],
        [new(0, 0), new(1, 0), new(-2, 0), new(1, 2), new(-2, -1)]
    ];

    private static readonly Cell[][] LineKicksCounterClockwise =
    [
        [new(0, 0), new(1, 0), new(-2, 0), new(1, 2), new(-2, -1)],
        [new(0, 0), new(2, 0), new(-1, 0), new(2, -1), new(-1, 2)],
        [new(0, 0), new(1, 0), new(-2, 0), new(1, 2), new(-2, -1)],
        [new(0, 0), new(-2, 0), new(1, 0), new(-2, 1), new(1, -2)]
    ];

    private sealed record Shape(int BoxSize, Cell[][] States, int TopRow)
    {
        /// <summary>
        /// Derives the other three states by turning the spawn state a quarter at a time, so only
        /// one layout per piece is written by hand and the cell order stays the same in every state.
        /// </summary>
        public static Shape FromSpawnState(int boxSize, Cell[] spawn)
        {
            var states = new Cell[RotationCount][];
            states[0] = spawn;

            for (int state = 1; state < RotationCount; state++)
            {
                var previous = states[state - 1];
                var turned = new Cell[previous.Length];

                for (int i = 0; i < previous.Length; i++)
                {
                    turned[i] = new Cell(boxSize - 1 - previous[i].Y, previous[i].X);
                }

                states[state] = turned;
            }

            var topRow = int.MaxValue;
            foreach (var cell in spawn)
            {
                topRow = Math.Min(topRow, cell.Y);
            }

            return new Shape(boxSize, states, topRow);
        }
    }
}
