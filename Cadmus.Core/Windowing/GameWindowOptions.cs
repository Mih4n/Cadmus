namespace Cadmus.Core.Windowing;

/// <summary>
/// Window configuration, registered as a singleton and injected into the window implementation.
/// </summary>
public sealed class GameWindowOptions
{
    public string Title { get; set; } = "Cadmus";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool Resizable { get; set; } = true;
    public bool VSync { get; set; } = true;
}
