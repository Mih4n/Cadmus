using System.Numerics;
using static System.FormattableString;
using Cadmus.Core.Diagnostics;
using Cadmus.Core.Game;
using Cadmus.Core.Input;
using Cadmus.Core.Systems;
using Cadmus.Engine;
using Cadmus.Engine.Geometry;
using Cadmus.Graphics;

namespace Cadmus.Rendering;

/// <summary>
/// The engine's on-screen statistics HUD. Text is drawn as one textured quad per glyph from the
/// bundled ASCII atlas, in screen space, on top of everything.
/// </summary>
public sealed class DebugOverlay : IDebugOverlay, ISystem
{
    private const float Depth = 1000f;

    private readonly IFrameStatistics statistics;
    private readonly IInputService input;
    private readonly BitmapFont font;
    private readonly string whiteTexturePath;
    private readonly List<RenderItem> items = [];

    private readonly Vector4 panelColor = Colors.FromSrgb(8, 10, 14, 190);
    private readonly Vector4 textColor = Colors.FromHex(0xD8E0EA);
    private readonly Vector4 headingColor = Colors.FromHex(0x8BD450);
    private readonly Vector4 warningColor = Colors.FromHex(0xE8A33A);

    public bool IsVisible { get; set; } = true;

    /// <summary>Height of one text line in pixels.</summary>
    public float LineHeight { get; set; } = 16f;

    public float Margin { get; set; } = 12f;

    /// <summary>Toggled by this key; runs late so it sees this frame's input.</summary>
    public int Order => int.MaxValue - 1;

    public DebugOverlay(IFrameStatistics statistics, IInputService input, VulkanOptions options)
    {
        this.statistics = statistics;
        this.input = input;

        font = BitmapFont.CreateDefault(options.FontTexturePath);
        whiteTexturePath = options.WhiteTexturePath;
    }

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        if (input.WasKeyPressed(Key.F3))
        {
            IsVisible = !IsVisible;
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>Builds the HUD's draw items for a framebuffer of the given size.</summary>
    public IReadOnlyList<RenderItem> Build(int width, int height)
    {
        items.Clear();

        if (!IsVisible)
        {
            return items;
        }

        var lines = BuildLines();
        var glyphWidth = LineHeight * font.Aspect;

        var longest = 0;
        foreach (var (text, _) in lines)
        {
            longest = Math.Max(longest, text.Length);
        }

        var panelWidth = longest * glyphWidth + Margin * 2;
        var panelHeight = lines.Count * LineHeight + Margin * 2;

        AddPanel(panelWidth, panelHeight);

        var y = Margin;
        foreach (var (text, color) in lines)
        {
            AddText(text, new Vector2(Margin, y), glyphWidth, color);
            y += LineHeight;
        }

        return items;
    }

    private List<(string Text, Vector4 Color)> BuildLines()
    {
        var (width, height) = statistics.Resolution;

        // Frame time matters more than FPS when hunting stutter, so show both.
        var frameTimeColor = statistics.MaxFrameTimeMs > 33f ? warningColor : textColor;

        return
        [
            ("CADMUS  F3 to hide", headingColor),
            (Invariant($"fps        {statistics.Fps,7:F1}"), textColor),
            (Invariant($"frame ms   {statistics.FrameTimeMs,7:F2}"), textColor),
            (Invariant($"  min/max  {statistics.MinFrameTimeMs,5:F2} /{statistics.MaxFrameTimeMs,6:F2}"), frameTimeColor),
            (Invariant($"frame      {statistics.FrameIndex,7}"), textColor),
            (Invariant($"uptime     {statistics.UptimeSeconds,6:F1}s"), textColor),
            ("", textColor),
            (Invariant($"scene      {statistics.SceneName}"), headingColor),
            (Invariant($"entities   {statistics.SceneEntities,7}"), textColor),
            (Invariant($"draws      {statistics.DrawCalls,7}"), textColor),
            ("", textColor),
            (Invariant($"textures   {statistics.CachedTextures,7}"), textColor),
            (Invariant($"meshes     {statistics.CachedMeshes,7}"), textColor),
            (Invariant($"target     {width}x{height}"), textColor),
            (statistics.DeviceName, headingColor)
        ];
    }

    private void AddPanel(float width, float height)
    {
        var model = Matrix4x4.CreateScale(width, height, 1f)
                  * Matrix4x4.CreateTranslation(width / 2f, height / 2f, Depth);

        items.Add(
            new RenderItem(
                Mesh.UnitQuad,
                whiteTexturePath,
                model,
                panelColor,
                Depth,
                ScreenSpace: true
            )
        );
    }

    private void AddText(string text, Vector2 position, float glyphWidth, Vector4 color)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var glyph = font.GetGlyph(text[i]);
            if (glyph is null || text[i] == ' ')
            {
                continue;
            }

            var model = Matrix4x4.CreateScale(glyphWidth, LineHeight, 1f)
                      * Matrix4x4.CreateTranslation(
                            position.X + i * glyphWidth + glyphWidth / 2f,
                            position.Y + LineHeight / 2f,
                            Depth + 1f
                        );

            items.Add(
                new RenderItem(
                    glyph,
                    font.TexturePath,
                    model,
                    color,
                    Depth + 1f,
                    ScreenSpace: true
                )
            );
        }
    }
}
