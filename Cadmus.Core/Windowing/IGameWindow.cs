namespace Cadmus.Core.Windowing;

/// <summary>
/// The engine-facing view of the platform window. Deliberately free of any Silk.NET type so the
/// contracts assembly stays backend-agnostic; the Vulkan backend supplies the implementation.
/// </summary>
public interface IGameWindow
{
    string Title { get; set; }

    /// <summary>Size of the drawable surface in pixels.</summary>
    (int Width, int Height) FramebufferSize { get; }

    bool IsClosing { get; }

    /// <summary>True while the window is minimised — no frames should be submitted.</summary>
    bool IsMinimized { get; }

    void PollEvents();
    void Close();

    event Action<(int Width, int Height)>? Resized;
}
