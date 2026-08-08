using Cadmus.Core.Windowing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Cadmus.Graphics.Windowing;

/// <summary>
/// Silk.NET-backed window. Created eagerly (the Vulkan instance needs its surface extensions) and
/// handed to the rest of the engine as <see cref="IGameWindow"/>.
/// </summary>
public sealed class SilkGameWindow : IGameWindow, IDisposable
{
    private readonly GameWindowOptions options;

    /// <summary>The underlying Silk window — only the Vulkan backend should reach for this.</summary>
    public IWindow Native { get; }

    public event Action<(int Width, int Height)>? Resized;

    public SilkGameWindow(GameWindowOptions options)
    {
        this.options = options;

        var windowOptions = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(options.Width, options.Height),
            Title = options.Title,
            WindowBorder = options.Resizable ? WindowBorder.Resizable : WindowBorder.Fixed,
            VSync = options.VSync,
            IsVisible = true
        };

        try
        {
            Native = Window.Create(windowOptions);
            Native.Initialize();
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or DllNotFoundException)
        {
            // The usual cause is a shell without the GLFW/X11/Wayland libraries on LD_LIBRARY_PATH:
            // GLFW dlopens them, and the copy Silk.NET bundles carries no RPATH.
            throw new PlatformNotSupportedException(
                "No windowing platform is available. Run inside the project's dev shell "
                + "(`devenv shell`, or `direnv allow` once) so GLFW and the Vulkan loader are on "
                + "LD_LIBRARY_PATH. `devenv shell doctor` reports what is missing.",
                exception);
        }

        if (Native.VkSurface is null)
        {
            throw new PlatformNotSupportedException("The windowing platform does not support Vulkan.");
        }

        Native.FramebufferResize += size => Resized?.Invoke((size.X, size.Y));
    }

    public string Title
    {
        get => Native.Title;
        set => Native.Title = value;
    }

    public (int Width, int Height) FramebufferSize => (Native.FramebufferSize.X, Native.FramebufferSize.Y);

    public bool IsClosing => Native.IsClosing;

    public bool IsMinimized => Native.FramebufferSize.X == 0 || Native.FramebufferSize.Y == 0;

    public void PollEvents() => Native.DoEvents();

    public void Close() => Native.Close();

    public void Dispose()
    {
        Native.DoEvents();
        Native.Reset();
        Native.Dispose();
    }
}
