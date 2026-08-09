using Cadmus.Core.Components;

namespace Cadmus.Engine.Components;

public class Component : IComponent
{
    public bool IsActive { get; set; }
}