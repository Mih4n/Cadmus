using Cadmus.Core.Events;
using Cadmus.Core.Game;
using Cadmus.Core.Input;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Engine.Scenes;
using TestGame.Snake.Components;
using TestGame.Snake.Events;

namespace TestGame.Snake.Systems;

/// <summary>
/// Turns keyboard state into intent. It queries every entity carrying a
/// <see cref="PlayerControlComponent"/> — entities never read the input service themselves — and
/// publishes what the player asked for, leaving the decision of what that means to other systems.
/// </summary>
public sealed class PlayerInputSystem(
    ISceneManager scenes,
    IInputService input,
    IEventQueue events
) : ISystem
{
    public int Order => SystemOrder.Input;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        foreach (var (entity, control, body) in scenes.Current.Query<PlayerControlComponent, SnakeBodyComponent>())
        {
            var requested = ReadHeading(control);

            // A turn into the neck would kill the snake on its own body, so it is never a valid
            // request; comparing against the committed heading also makes a fast double-tap safe.
            if (requested is { } heading && !heading.IsOpposite(body.Heading))
            {
                control.RequestedHeading = heading;
                events.Publish(new TurnRequested(entity, heading));
            }
        }

        if (input.WasKeyPressed(Key.P))
        {
            events.Publish(new PauseToggled());
        }

        if (input.WasKeyPressed(Key.R) || input.WasKeyPressed(Key.Space) || input.WasKeyPressed(Key.Enter))
        {
            events.Publish(new RestartRequested());
        }

        return ValueTask.CompletedTask;
    }

    private Cell? ReadHeading(PlayerControlComponent control)
    {
        if (AnyPressed(control.Left)) return Cell.Left;
        if (AnyPressed(control.Right)) return Cell.Right;
        if (AnyPressed(control.Up)) return Cell.Up;
        if (AnyPressed(control.Down)) return Cell.Down;

        return null;
    }

    private bool AnyPressed(Key[] keys)
    {
        foreach (var key in keys)
        {
            if (input.WasKeyPressed(key))
            {
                return true;
            }
        }

        return false;
    }
}
