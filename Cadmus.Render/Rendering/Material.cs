using Cadmus.Domain.Contracts.Rendering;

namespace Cadmus.Render.Rendering;

public class Material
{
    public IShader Shader { get; }
    public object[] Textures { get; }

    public Material(IShader shader, params object[] textures)
    {
        Shader = shader;
        Textures = textures ?? [];
    }
}