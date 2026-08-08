using Cadmus.App;
using Cadmus.Core.Entities;
using Cadmus.Core.Game;
using Cadmus.Core.Input;
using Cadmus.Core.Scenes;
using Cadmus.Core.Windowing;
using Cadmus.Rendering;

namespace TestGame;

/// <summary>
/// Owns the application-level concerns: loading the scene, quitting, and the unattended screenshot
/// path. The gameplay itself lives in <c>SnakeScene</c>.
/// </summary>
public sealed class SnakeGame(
    ISceneManager scenes,
    IEntityFactory entities,
    IGameWindow window,
    IInputService input,
    VulkanRenderSystem renderer
) : Game(scenes, entities, window)
{
    private readonly string? screenshotPath = Environment.GetEnvironmentVariable("CADMUS_SCREENSHOT");
    private readonly long screenshotFrame =
        long.TryParse(Environment.GetEnvironmentVariable("CADMUS_SCREENSHOT_FRAME"), out var frame) ? frame : 90;

    public override Task InitializeAsync(CancellationToken cancellationToken = default) =>
        LoadSceneAsync("Game", cancellationToken);

    public override Task UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        if (input.WasKeyPressed(Key.Escape))
        {
            Window.Close();
        }

        CaptureScreenshotIfRequested(time);

        return Task.CompletedTask;
    }

    /// <summary>Smoke test for CI and for sessions without a visible display.</summary>
    private void CaptureScreenshotIfRequested(GameTime time)
    {
        if (screenshotPath is null)
        {
            return;
        }

        if (time.FrameIndex == screenshotFrame)
        {
            renderer.RequestCapture(screenshotPath);
        }
        else if (time.FrameIndex > screenshotFrame + 5)
        {
            // Closing the window ends the loop; the game never needs the host itself.
            Window.Close();
        }
    }
}
