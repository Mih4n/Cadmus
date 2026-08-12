using System.Numerics;
using Cadmus.Engine;

namespace Tetris;

/// <summary>
/// Tuning for the game, registered as a singleton and injected into the scene and its systems rather
/// than read from statics.
/// </summary>
public sealed class TetrisSettings
{
    public int Columns { get; init; } = 10;
    public int Rows { get; init; } = 20;
    public float CellSize { get; init; } = 34f;

    /// <summary>Gap between a cell's edge and its block, in pixels.</summary>
    public float CellPadding { get; init; } = 3f;

    /// <summary>Seconds per row at level 1; every level multiplies it by <see cref="GravityFalloff"/>.</summary>
    public float StartGravitySeconds { get; init; } = 0.75f;
    public float MinGravitySeconds { get; init; } = 0.05f;
    public float GravityFalloff { get; init; } = 0.82f;

    /// <summary>How much faster gravity runs while soft drop is held.</summary>
    public float SoftDropMultiplier { get; init; } = 16f;

    /// <summary>Floor on the soft-drop interval, so a high level cannot make it a hard drop.</summary>
    public float MinSoftDropSeconds { get; init; } = 0.018f;

    /// <summary>Grace period between touching down and locking, which is what lets a piece be slid
    /// into place at the last moment.</summary>
    public float LockDelaySeconds { get; init; } = 0.4f;

    /// <summary>How many moves may refresh the lock delay, so a piece cannot be kept alive forever.</summary>
    public int MaxLockResets { get; init; } = 12;

    /// <summary>Held-key auto-repeat: the wait before the second shift, then the rate.</summary>
    public float RepeatDelaySeconds { get; init; } = 0.16f;
    public float RepeatIntervalSeconds { get; init; } = 0.035f;

    /// <summary>Full rows glow and shrink for this long, then everything above slides down.</summary>
    public float FlashSeconds { get; init; } = 0.16f;
    public float CollapseSeconds { get; init; } = 0.13f;

    /// <summary>
    /// Time constant of the drawn piece catching up with the logical one: after this many seconds
    /// roughly two thirds of a sideways move or wall kick has been travelled. The fall itself needs
    /// no smoothing — the gravity timer already says how far into the row the piece is.
    /// </summary>
    public float SmoothingSeconds { get; init; } = 0.045f;

    public int LinesPerLevel { get; init; } = 10;

    /// <summary>How many upcoming pieces the side panel shows.</summary>
    public int PreviewCount { get; init; } = 4;

    /// <summary>Cell size of the pieces drawn in the side panels.</summary>
    public float PreviewCellSize => CellSize * 0.6f;

    public float PanelGap => CellSize * 0.8f;
    public float PanelWidth => PreviewCellSize * 6f;

    public float LineHeight { get; init; } = 20f;

    public float BoardWidth => Columns * CellSize;
    public float BoardHeight => Rows * CellSize;

    /// <summary>Thickness of the frame drawn around the well, in pixels.</summary>
    public float BorderWidth { get; init; } = 4f;

    /// <summary>Everything the layout occupies, used to centre it in the window.</summary>
    public float LayoutWidth => BoardWidth + (PanelWidth + PanelGap) * 2f;

    /// <summary>Sprite size of one block, leaving the padding as a grid gap.</summary>
    public Vector2 BlockSize => new(CellSize - CellPadding, CellSize - CellPadding);

    public Vector2 PreviewBlockSize => new(PreviewCellSize - CellPadding, PreviewCellSize - CellPadding);

    // Authored as sRGB and converted: shader tints are linear, see Colors.
    public Vector4 BoardColor { get; init; } = Colors.FromHex(0x151A24);
    public Vector4 BorderColor { get; init; } = Colors.FromHex(0x2E3745);
    public Vector4 GridColor { get; init; } = Colors.FromSrgb(120, 140, 170, 26);
    public Vector4 PanelColor { get; init; } = Colors.FromSrgb(20, 26, 36, 200);
    public Vector4 TextColor { get; init; } = Colors.FromHex(0xD8E0EA);
    public Vector4 LabelColor { get; init; } = Colors.FromHex(0x7C8BA1);
    public Vector4 AccentColor { get; init; } = Colors.FromHex(0x6FD3E8);
    public Vector4 WarningColor { get; init; } = Colors.FromHex(0xE86A5A);

    /// <summary>Tint of the landing preview: the piece's own colour, mostly transparent.</summary>
    public float GhostAlpha { get; init; } = 0.22f;

    /// <summary>Dim laid over the well while the match is paused or over.</summary>
    public Vector4 OverlayColor { get; init; } = Colors.FromSrgb(8, 10, 14, 180);

    public Vector4 ColorOf(PieceKind kind) => kind switch
    {
        PieceKind.I => Colors.FromHex(0x38C7D9),
        PieceKind.J => Colors.FromHex(0x4A7BE8),
        PieceKind.L => Colors.FromHex(0xE8913A),
        PieceKind.O => Colors.FromHex(0xE8C63A),
        PieceKind.S => Colors.FromHex(0x66C94A),
        PieceKind.T => Colors.FromHex(0xB05CE0),
        _ => Colors.FromHex(0xE0503A)
    };

    /// <summary>Seconds between rows at the given level. Gravity and presentation share it.</summary>
    public float GravitySecondsFor(int level) => MathF.Max(
        MinGravitySeconds,
        StartGravitySeconds * MathF.Pow(GravityFalloff, level - 1)
    );

    public float SoftDropSecondsFor(int level) => MathF.Max(
        MinSoftDropSeconds,
        GravitySecondsFor(level) / SoftDropMultiplier
    );

    /// <summary>Classic single/double/triple/tetris values, multiplied by the level when awarded.</summary>
    public int ScoreForLines(int count) => count switch
    {
        1 => 100,
        2 => 300,
        3 => 500,
        >= 4 => 800,
        _ => 0
    };

    /// <summary>
    /// Where a piece enters the well: centred, and pushed down just far enough that every block of
    /// its spawn state is inside the visible field.
    /// </summary>
    public Cell SpawnCell(PieceKind kind) => new(
        (Columns - Tetromino.BoxSize(kind)) / 2,
        -Tetromino.TopRow(kind)
    );

    /// <summary>Pixel position of a point given in cell units, relative to the well's top-left corner.</summary>
    public Vector3 PointAt(Vector2 cellPoint, float depth) => new(
        cellPoint.X * CellSize,
        cellPoint.Y * CellSize,
        depth
    );

    /// <summary>Centre of a grid cell, which may be fractional while something is being animated.</summary>
    public Vector3 CellCenter(Vector2 cell, float depth) => PointAt(
        new Vector2(cell.X + 0.5f, cell.Y + 0.5f),
        depth
    );

    public Vector3 CellCenter(Cell cell, float depth) => CellCenter(new Vector2(cell.X, cell.Y), depth);

    public bool ContainsColumn(int column) => column >= 0 && column < Columns;
}
