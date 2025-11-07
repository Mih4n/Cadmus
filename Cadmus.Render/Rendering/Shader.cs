using System.Numerics;
using Cadmus.Domain.Contracts.Rendering;

namespace Cadmus.Render.Rendering;

public class Shader : IShader, IDisposable
{
    public required string Name { get; set; }
    public List<object> Textures { get; set; } = [];
    public Matrix4x4 Model { get; set; }
    public Matrix4x4 Projection { get; set; }

    public void BindTextures(object[] Textures)
    {
    }

    public void Dispose()
    {
    }
}