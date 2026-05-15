using Silk.NET.Vulkan;

namespace Cadmus.Render;

public unsafe class VulkanRenderPass : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;

    public RenderPass RenderPass { get; private set; }
    public Format DepthFormat { get; private set; }

    public VulkanRenderPass(Vk vk, VulkanDevice device, Format swapchainImageFormat)
    {
        _vk = vk;
        _device = device;
        DepthFormat = device.FindDepthFormat();
        CreateRenderPass(swapchainImageFormat);
    }

    private void CreateRenderPass(Format swapchainImageFormat)
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = swapchainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        AttachmentDescription depthAttachment = new()
        {
            Format = DepthFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        AttachmentReference depthAttachmentRef = new()
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal
        };

        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef
        };

        SubpassDependency dependency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
        };

        var attachments = stackalloc AttachmentDescription[] { colorAttachment, depthAttachment };

        RenderPassCreateInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 2,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency
        };

        if (_vk.CreateRenderPass(_device.Device, in renderPassInfo, null, out RenderPass renderPass) != Result.Success)
        {
            throw new Exception("Failed to create render pass!");
        }
        RenderPass = renderPass;
    }

    public void Dispose()
    {
        _vk.DestroyRenderPass(_device.Device, RenderPass, null);
    }
}
