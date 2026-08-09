using Cadmus.Core.Entities;

namespace TestGame.Snake.Events;

/// <summary>The player asked a snake to turn. Published by input, applied by movement.</summary>
public readonly record struct TurnRequested(IEntity Snake, Cell Heading);

/// <summary>A snake's head entered the cell holding food.</summary>
public readonly record struct FoodEaten(IEntity Snake, IEntity Food, Cell Cell);

/// <summary>A snake ran into a wall or into itself.</summary>
public readonly record struct SnakeDied(IEntity Snake, Cell Cell, DeathCause Cause);

public enum DeathCause
{
    Wall,
    Self
}

/// <summary>Match-level commands, so the systems that act on them need no key handling.</summary>
public readonly record struct RestartRequested;

public readonly record struct PauseToggled;

/// <summary>A match actually restarted; published by the flow system once the board is reset.</summary>
public readonly record struct MatchRestarted;
