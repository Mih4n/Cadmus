using Veldrid;

namespace Cadmus.Render.Rendering;

public class Material
{
    public Texture[] Textures { get; }

    public Material(params Texture[] textures)
    {
        Textures = textures ?? [];
    }
}