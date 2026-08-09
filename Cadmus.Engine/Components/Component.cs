using Cadmus.Core.Components;

namespace Cadmus.Engine.Components;

public class Component : IComponent
{
    /// <summary>
    /// True unless something switches it off. A component that has just been added is expected to
    /// work, so the default cannot be <c>false</c>.
    /// </summary>
    public bool IsActive { get; set; } = true;
}