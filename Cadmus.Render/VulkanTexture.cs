using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Silk.NET.Vulkan;

namespace Cadmus.Render;

public unsafe class VulkanTexture : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;

    public VulkanImage VulkanImage { get; }
    public Sampler Sampler { get; }

    public VulkanTexture(Vk vk, VulkanDevice device, string path)
    {
        _vk = vk;
        _device = device;

        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
        image.Mutate(x => x.Flip(FlipMode.Vertical));

        uint width = (uint)image.Width;
        uint height = (uint)image.Height;
        ulong imageSize = (ulong)(width * height * 4);

        // Staging buffer
        var stagingBuffer = new VulkanBuffer(vk, device, imageSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        // Copy pixel data to staging buffer
        image.ProcessPixelRows(accessor =>
        {
            void* data = stagingBuffer.Map();
            byte* dst = (byte*)data;
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                fixed (Rgba32* src = row)
                {
                    System.Buffer.MemoryCopy(src, dst + (y * accessor.Width * 4), accessor.Width * 4, accessor.Width * 4);
                }
            }
            stagingBuffer.Unmap();
        });

        // Create image
        VulkanImage = new VulkanImage(vk, device, width, height, Format.R8G8B8A8Srgb, ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ImageAspectFlags.ColorBit);

        // Transition + copy + transition
        TransitionImageLayout(VulkanImage.Image, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer.Buffer, VulkanImage.Image, width, height);
        TransitionImageLayout(VulkanImage.Image, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        stagingBuffer.Dispose();

        // Create sampler
        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = true,
            MaxAnisotropy = 16,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
            MipLodBias = 0,
            MinLod = 0,
            MaxLod = 0
        };

        if (_vk.CreateSampler(_device.Device, in samplerInfo, null, out Sampler sampler) != Result.Success)
        {
            throw new Exception("Failed to create texture sampler!");
        }
        Sampler = sampler;
    }

    private void TransitionImageLayout(Silk.NET.Vulkan.Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _device.CommandPool,
            CommandBufferCount = 1
        };

        _vk.AllocateCommandBuffers(_device.Device, in allocInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        _vk.BeginCommandBuffer(commandBuffer, in beginInfo);

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;
            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new Exception("Unsupported layout transition!");
        }

        _vk.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, in barrier);

        _vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        _vk.QueueSubmit(_device.GraphicsQueue, 1, in submitInfo, default);
        _vk.QueueWaitIdle(_device.GraphicsQueue);

        _vk.FreeCommandBuffers(_device.Device, _device.CommandPool, 1, in commandBuffer);
    }

    private void CopyBufferToImage(Silk.NET.Vulkan.Buffer buffer, Silk.NET.Vulkan.Image image, uint width, uint height)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _device.CommandPool,
            CommandBufferCount = 1
        };

        _vk.AllocateCommandBuffers(_device.Device, in allocInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        _vk.BeginCommandBuffer(commandBuffer, in beginInfo);

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            ImageOffset = new Offset3D { X = 0, Y = 0, Z = 0 },
            ImageExtent = new Extent3D { Width = width, Height = height, Depth = 1 }
        };

        _vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, in region);

        _vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        _vk.QueueSubmit(_device.GraphicsQueue, 1, in submitInfo, default);
        _vk.QueueWaitIdle(_device.GraphicsQueue);

        _vk.FreeCommandBuffers(_device.Device, _device.CommandPool, 1, in commandBuffer);
    }

    public void Dispose()
    {
        _vk.DestroySampler(_device.Device, Sampler, null);
        VulkanImage.Dispose();
    }
}
