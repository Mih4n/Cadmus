namespace Cadmus.Core.Diagnostics;

/// <summary>The engine's statistics HUD. Toggled with F3 by default.</summary>
public interface IDebugOverlay
{
    bool IsVisible { get; set; }
}
