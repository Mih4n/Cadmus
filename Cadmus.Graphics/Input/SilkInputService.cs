using Cadmus.Core.Game;
using Cadmus.Core.Input;
using Cadmus.Core.Systems;
using Cadmus.Graphics.Windowing;
using Silk.NET.Input;
using SilkKey = Silk.NET.Input.Key;

namespace Cadmus.Graphics.Input;

/// <summary>
/// Silk.NET keyboard state, sampled once per frame. It is also an <see cref="ISystem"/> so the host
/// refreshes it before any gameplay system runs — hence the lowest possible order.
/// </summary>
public sealed class SilkInputService : IInputService, ISystem, IDisposable
{
    private static readonly (Core.Input.Key Key, SilkKey Silk)[] KeyMap =
    [
        (Core.Input.Key.Left, SilkKey.Left),
        (Core.Input.Key.Right, SilkKey.Right),
        (Core.Input.Key.Up, SilkKey.Up),
        (Core.Input.Key.Down, SilkKey.Down),
        (Core.Input.Key.A, SilkKey.A),
        (Core.Input.Key.D, SilkKey.D),
        (Core.Input.Key.S, SilkKey.S),
        (Core.Input.Key.W, SilkKey.W),
        (Core.Input.Key.P, SilkKey.P),
        (Core.Input.Key.R, SilkKey.R),
        (Core.Input.Key.Space, SilkKey.Space),
        (Core.Input.Key.Enter, SilkKey.Enter),
        (Core.Input.Key.Escape, SilkKey.Escape),
        (Core.Input.Key.F1, SilkKey.F1),
        (Core.Input.Key.F3, SilkKey.F3)
    ];

    private readonly IInputContext context;
    private readonly HashSet<Core.Input.Key> current = [];
    private readonly HashSet<Core.Input.Key> previous = [];

    /// <summary>
    /// Runs right after the event queue is emptied, so every other system sees a keyboard state that
    /// matches this frame.
    /// </summary>
    public int Order => int.MinValue + 1;

    public SilkInputService(SilkGameWindow window)
    {
        context = window.Native.CreateInput();
    }

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        previous.Clear();
        previous.UnionWith(current);
        current.Clear();

        // Polled rather than event-driven: the host pumps the window immediately before this runs,
        // so a poll cannot miss or duplicate a transition within the frame.
        if (context.Keyboards.Count > 0)
        {
            var keyboard = context.Keyboards[0];

            foreach (var (key, silk) in KeyMap)
            {
                if (keyboard.IsKeyPressed(silk))
                {
                    current.Add(key);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    public bool IsKeyDown(Core.Input.Key key) => current.Contains(key);

    public bool WasKeyPressed(Core.Input.Key key) => current.Contains(key) && !previous.Contains(key);

    public bool WasKeyReleased(Core.Input.Key key) => !current.Contains(key) && previous.Contains(key);

    public void Dispose()
    {
        try
        {
            context.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Closing the window already tears the input context down; disposing must not throw.
        }
    }
}
