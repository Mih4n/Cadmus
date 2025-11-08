using Cadmus.Domain.Contracts.Components;
using Veldrid;
using Veldrid.Sdl2;

namespace Cadmus.Domain.Components.Rendering;

public class VulkanRenderingContextComponent : IComponent
{
    Sdl2Window Window { get; set; }
    CommandList Commands { get; set; }
    GraphicsDevice Device { get; set; }

    public VulkanRenderingContextComponent(SdlW)
}
