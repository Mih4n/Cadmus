using Cadmus.Domain.Contracts.Components;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Cadmus.Domain.Components.Rendering;

public class VulkanRenderingContext : IComponent, IDisposable
{
    public Vk Vulkan = null!;
    public IWindow Window = null!;

    public VulkanRenderingContext()
    {
        InitVulkan();
        InitWindow();
    }

    public void InitWindow()
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(600, 800),
            Title = "Cadmus"
        };

        Window = Silk.NET.Windowing.Window.Create(options);
        Window.Initialize();

        if (Window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }

    public void InitVulkan()
    {
        Vulkan = Vk.GetApi();
    }

    public void Dispose()
    {
        Window.Dispose();
        Vulkan.Dispose();
    }
}
