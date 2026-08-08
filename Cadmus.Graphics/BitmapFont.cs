using System.Numerics;
using Cadmus.Engine.Geometry;

namespace Cadmus.Graphics;

/// <summary>
/// A fixed-cell ASCII atlas. Each glyph is a quad mesh with its own UV rectangle, built once and
/// cached — the GPU cache keys meshes by reference, so a glyph uploads a single time.
/// </summary>
public sealed class BitmapFont
{
    private readonly Mesh?[] glyphs;

    public string TexturePath { get; }

    /// <summary>Glyph cell in atlas pixels.</summary>
    public Vector2 CellSize { get; }

    /// <summary>Width-to-height ratio of a glyph, used to lay text out from a line height.</summary>
    public float Aspect => CellSize.X / CellSize.Y;

    public char FirstChar { get; }
    public char LastChar { get; }

    public BitmapFont(
        string texturePath,
        Vector2 atlasSize,
        Vector2 cellSize,
        int columns,
        char firstChar = ' ',
        char lastChar = '~')
    {
        TexturePath = texturePath;
        CellSize = cellSize;
        FirstChar = firstChar;
        LastChar = lastChar;

        glyphs = new Mesh?[lastChar - firstChar + 1];

        for (int i = 0; i < glyphs.Length; i++)
        {
            var column = i % columns;
            var row = i / columns;

            var min = new Vector2(column * cellSize.X / atlasSize.X, row * cellSize.Y / atlasSize.Y);
            var max = new Vector2((column + 1) * cellSize.X / atlasSize.X, (row + 1) * cellSize.Y / atlasSize.Y);

            glyphs[i] = Mesh.CreateQuad(min, max);
        }
    }

    /// <summary>The quad for a character, or null when it is outside the atlas.</summary>
    public Mesh? GetGlyph(char character)
    {
        if (character < FirstChar || character > LastChar)
        {
            return null;
        }

        return glyphs[character - FirstChar];
    }

    /// <summary>The default 256x144 atlas shipped with the engine: ASCII 32-126 in 16x24 cells.</summary>
    public static BitmapFont CreateDefault(string texturePath) => new(
        texturePath,
        new Vector2(256, 144),
        new Vector2(16, 24),
        columns: 16
    );
}
