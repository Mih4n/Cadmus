using System.Numerics;
using Cadmus.Core.Components;

namespace Cadmus.Engine.Components;

/// <summary>
/// Describes *what* to draw with, never *how* it lives on the GPU — the uploaded texture is owned by
/// the render layer's resource cache and keyed by <see cref="TexturePath"/>.
/// </summary>
public class MaterialComponent(string texturePath) : IComponent
{
    public string TexturePath { get; set; } = texturePath;
    public Vector4 Tint { get; set; } = Vector4.One;
}
