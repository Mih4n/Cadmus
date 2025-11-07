using System.Numerics;

namespace Cadmus.Domain.Contracts.Rendering;

public interface IShader
{
    string Name { get; set; }
    Matrix4x4 Model { get; set; }
    Matrix4x4 Projection { get; set; }
    List<object> Textures { get; set; }

    public void BindTextures(object[] Textures);
}