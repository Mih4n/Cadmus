using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

/// <summary>
/// Depth buffer plus one framebuffer per swapchain image. Rebuilds itself whenever the swapchain is
/// recreated, so nothing outside has to coordinate the resize.
/// </summary>
public sealed unsafe class VulkanFramebuffers : IDisposable
{
    private readonly VulkanDevice device;
    private readonly VulkanSwapchain swapchain;
    private readonly VulkanRenderPass renderPass;

    private VulkanImage depthImage = null!;
    private bool disposed;

    public Framebuffer[] Handles { get; private set; } = [];

    public VulkanFramebuffers(VulkanDevice device, VulkanSwapchain swapchain, VulkanRenderPass renderPass)
    {
        this.device = device;
        this.swapchain = swapchain;
        this.renderPass = renderPass;

        Build();

        swapchain.Recreated += Rebuild;
    }

    private void Rebuild()
    {
        Destroy();
        Build();
    }

    private void Build()
    {
        depthImage = new VulkanImage(
            device,
            swapchain.Extent.Width,
            swapchain.Extent.Height,
            renderPass.DepthFormat,
            ImageTiling.Optimal,
            ImageUsageFlags.DepthStencilAttachmentBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ImageAspectFlags.DepthBit);

        Handles = new Framebuffer[swapchain.ImageViews.Length];
        var attachments = stackalloc ImageView[2];

        for (int i = 0; i < swapchain.ImageViews.Length; i++)
        {
            attachments[0] = swapchain.ImageViews[i];
            attachments[1] = depthImage.View;

            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = renderPass.Handle,
                AttachmentCount = 2,
                PAttachments = attachments,
                Width = swapchain.Extent.Width,
                Height = swapchain.Extent.Height,
                Layers = 1
            };

            if (device.Api.CreateFramebuffer(device.Handle, in framebufferInfo, null, out Handles[i]) != Result.Success)
            {
                throw new InvalidOperationException("Failed to create a framebuffer.");
            }
        }
    }

    private void Destroy()
    {
        foreach (var framebuffer in Handles)
        {
            device.Api.DestroyFramebuffer(device.Handle, framebuffer, null);
        }

        Handles = [];
        depthImage?.Dispose();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        swapchain.Recreated -= Rebuild;
        Destroy();
        disposed = true;
    }
}
