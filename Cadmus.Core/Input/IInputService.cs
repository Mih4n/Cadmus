namespace Cadmus.Core.Input;

/// <summary>
/// Keyboard state for the current frame. Injected like any other service; the implementation
/// refreshes itself as a system that runs before everything else in the frame.
/// </summary>
public interface IInputService
{
    /// <summary>True while the key is held.</summary>
    bool IsKeyDown(Key key);

    /// <summary>True only on the frame the key went down — use this for menu/direction input.</summary>
    bool WasKeyPressed(Key key);

    /// <summary>True only on the frame the key went up.</summary>
    bool WasKeyReleased(Key key);
}
