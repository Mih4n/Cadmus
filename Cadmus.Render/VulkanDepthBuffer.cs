using Silk.NET.Vulkan;

namespace Cadmus.Render;

public class VulkanDepthBuffer : IDisposable
{
    public VulkanImage Image { get; }

    public VulkanDepthBuffer(Vk vk, VulkanDevice device, uint width, uint height)
    {
        var depthFormat = device.FindDepthFormat();
        Image = new VulkanImage(vk, device, width, height, depthFormat, ImageTiling.Optimal,
            ImageUsageFlags.DepthStencilAttachmentBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ImageAspectFlags.DepthBit);
    }

    public void Dispose()
    {
        Image.Dispose();
    }
}
