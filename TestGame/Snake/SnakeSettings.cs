using System.Numerics;
using Cadmus.Engine;

namespace TestGame.Snake;

/// <summary>
/// Tuning for the demo, registered as a singleton and injected into the scene and its entities
/// rather than read from statics.
/// </summary>
public sealed class SnakeSettings
{
    public int Columns { get; init; } = 28;
    public int Rows { get; init; } = 20;
    public float CellSize { get; init; } = 28f;

    /// <summary>Gap between a cell's edge and its sprite, in pixels.</summary>
    public float CellPadding { get; init; } = 3f;

    public int StartLength { get; init; } = 4;

    public float StartTickSeconds { get; init; } = 0.14f;
    public float MinTickSeconds { get; init; } = 0.06f;

    /// <summary>How much each eaten apple shortens the tick.</summary>
    public float TickSpeedUp { get; init; } = 0.004f;

    /// <summary>
    /// Glide the sprites between cells instead of snapping. The rules stay on the grid either way —
    /// this only affects how the move is drawn.
    /// </summary>
    public bool SmoothMovement { get; init; } = true;

    /// <summary>Seconds between moves at the given score. Movement and presentation share it.</summary>
    public float TickSecondsFor(int score) => MathF.Max(
        MinTickSeconds,
        StartTickSeconds - score * TickSpeedUp
    );

    // Authored as sRGB and converted: shader tints are linear, see Colors.
    public Vector4 BoardColor { get; init; } = Colors.FromHex(0x232A36);
    public Vector4 BorderColor { get; init; } = Colors.FromHex(0x3C4657);

    /// <summary>Thickness of the frame drawn around the play field, in pixels.</summary>
    public float BorderWidth { get; init; } = 4f;
    public Vector4 HeadColor { get; init; } = Colors.FromHex(0x9BE564);
    public Vector4 BodyColor { get; init; } = Colors.FromHex(0x54B04A);
    public Vector4 TailColor { get; init; } = Colors.FromHex(0x2F7A46);
    public Vector4 DeadColor { get; init; } = Colors.FromHex(0xB03A3A);
    public Vector4 FoodColor { get; init; } = Colors.FromHex(0xE8503A);

    public float BoardWidth => Columns * CellSize;
    public float BoardHeight => Rows * CellSize;

    /// <summary>Sprite size for one cell, leaving the padding as a grid gap.</summary>
    public Vector2 SpriteSize => new(CellSize - CellPadding, CellSize - CellPadding);

    /// <summary>Centre of the given cell, relative to the board's top-left corner.</summary>
    public Vector3 CellCenter(Cell cell, float depth) => new(
        (cell.X + 0.5f) * CellSize,
        (cell.Y + 0.5f) * CellSize,
        depth
    );

    public bool Contains(Cell cell) =>
        cell.X >= 0 && cell.X < Columns && cell.Y >= 0 && cell.Y < Rows;
}
