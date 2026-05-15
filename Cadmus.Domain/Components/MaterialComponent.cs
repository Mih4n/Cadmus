using Cadmus.Domain.Contracts.Components;

namespace Cadmus.Domain.Components;

public class MaterialComponent : IComponent
{
    public string TexturePath { get; }

    public MaterialComponent(string texturePath)
    {
        TexturePath = texturePath;
    }
}
