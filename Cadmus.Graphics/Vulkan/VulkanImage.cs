using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

public sealed unsafe class VulkanImage : IDisposable
{
    private readonly VulkanDevice device;

    public Image Handle { get; }
    public DeviceMemory Memory { get; }
    public ImageView View { get; }
    public uint Width { get; }
    public uint Height { get; }

    public VulkanImage(
        VulkanDevice device,
        uint width,
        uint height,
        Format format,
        ImageTiling tiling,
        ImageUsageFlags usage,
        MemoryPropertyFlags properties,
        ImageAspectFlags aspectFlags,
        SampleCountFlags samples = SampleCountFlags.Count1Bit)
    {
        this.device = device;
        Width = width;
        Height = height;

        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D { Width = width, Height = height, Depth = 1 },
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = samples,
            SharingMode = SharingMode.Exclusive
        };

        if (device.Api.CreateImage(device.Handle, in imageInfo, null, out var image) != Result.Success)
        {
            throw new InvalidOperationException("Failed to create an image.");
        }
        Handle = image;

        device.Api.GetImageMemoryRequirements(device.Handle, Handle, out var requirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = device.FindMemoryType(requirements.MemoryTypeBits, properties)
        };

        if (device.Api.AllocateMemory(device.Handle, in allocInfo, null, out var memory) != Result.Success)
        {
            throw new InvalidOperationException("Failed to allocate image memory.");
        }
        Memory = memory;

        device.Api.BindImageMemory(device.Handle, Handle, Memory, 0);

        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Handle,
            ViewType = ImageViewType.Type2D,
            Format = format,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = aspectFlags,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        if (device.Api.CreateImageView(device.Handle, in viewInfo, null, out var view) != Result.Success)
        {
            throw new InvalidOperationException("Failed to create an image view.");
        }
        View = view;
    }

    public void TransitionLayout(ImageLayout oldLayout, ImageLayout newLayout) => device.SubmitImmediate(commandBuffer =>
    {
        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = Handle,
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
            throw new NotSupportedException($"Unsupported layout transition {oldLayout} → {newLayout}.");
        }

        device.Api.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, in barrier);
    });

    public void CopyFrom(VulkanBuffer buffer) => device.SubmitImmediate(commandBuffer =>
    {
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
            ImageExtent = new Extent3D { Width = Width, Height = Height, Depth = 1 }
        };

        device.Api.CmdCopyBufferToImage(commandBuffer, buffer.Handle, Handle, ImageLayout.TransferDstOptimal, 1, in region);
    });

    public void Dispose()
    {
        device.Api.DestroyImageView(device.Handle, View, null);
        device.Api.DestroyImage(device.Handle, Handle, null);
        device.Api.FreeMemory(device.Handle, Memory, null);
    }
}
