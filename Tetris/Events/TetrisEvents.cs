using Cadmus.Core.Entities;

namespace Tetris.Events;

/// <summary>The player asked for a sideways step. Published by input, applied by piece control.</summary>
public readonly record struct MoveRequested(int Direction);

public readonly record struct RotateRequested(bool Clockwise);

public readonly record struct HardDropRequested;

public readonly record struct HoldRequested;

/// <summary>A piece came to rest. The well merges it in; nobody else has to know it happened.</summary>
public readonly record struct PieceLocked(IEntity Piece);

/// <summary>
/// Rows were completed. Published when they start flashing, so the score reacts at the same moment
/// the board does.
/// </summary>
public readonly record struct LinesCleared(int Count);

/// <summary>Cells a piece was dropped through, worth points; hard drops are worth double.</summary>
public readonly record struct PieceDropped(int Cells, bool IsHard);

/// <summary>A new piece did not fit where it enters the well.</summary>
public readonly record struct GameOver;

/// <summary>Match-level commands, so the systems that act on them need no key handling.</summary>
public readonly record struct RestartRequested;

public readonly record struct PauseToggled;
