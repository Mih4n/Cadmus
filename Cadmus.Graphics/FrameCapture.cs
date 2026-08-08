using Cadmus.Graphics.Vulkan;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Cadmus.Graphics;

/// <summary>Saves a presented swapchain image to disk.</summary>
public interface IFrameCapture
{
    void Capture(uint imageIndex, string path);
}

/// <summary>
/// Copies a presented swapchain image into a linear host-visible image and writes it out as PNG.
/// Stalls the device, so it is meant for screenshots and tests, not per-frame use.
/// </summary>
public sealed unsafe class FrameCapture(VulkanDevice device, VulkanSwapchain swapchain) : IFrameCapture
{
    public void Capture(uint imageIndex, string path)
    {
        if (!swapchain.SupportsCapture)
        {
            throw new NotSupportedException(
                "The surface does not allow presented images to be used as a transfer source, so frames cannot be captured.");
        }

        device.WaitIdle();

        var width = swapchain.Extent.Width;
        var height = swapchain.Extent.Height;
        var source = swapchain.Images[imageIndex];

        var (staging, memory) = CreateStagingImage(width, height);

        try
        {
            device.SubmitImmediate(commandBuffer =>
            {
                Barrier(
                    commandBuffer,
                    source,
                    ImageLayout.PresentSrcKhr,
                    ImageLayout.TransferSrcOptimal,
                    AccessFlags.MemoryReadBit,
                    AccessFlags.TransferReadBit
                );

                Barrier(
                    commandBuffer,
                    staging,
                    ImageLayout.Undefined,
                    ImageLayout.TransferDstOptimal,
                    0,
                    AccessFlags.TransferWriteBit
                );

                ImageCopy region = new()
                {
                    SrcSubresource = new ImageSubresourceLayers
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        MipLevel = 0,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    },
                    DstSubresource = new ImageSubresourceLayers
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        MipLevel = 0,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    },
                    Extent = new Extent3D { Width = width, Height = height, Depth = 1 }
                };

                device.Api.CmdCopyImage(
                    commandBuffer,
                    source,
                    ImageLayout.TransferSrcOptimal,
                    staging,
                    ImageLayout.TransferDstOptimal,
                    1,
                    in region
                );

                Barrier(
                    commandBuffer,
                    staging,
                    ImageLayout.TransferDstOptimal,
                    ImageLayout.General,
                    AccessFlags.TransferWriteBit,
                    AccessFlags.MemoryReadBit
                );

                Barrier(
                    commandBuffer,
                    source,
                    ImageLayout.TransferSrcOptimal,
                    ImageLayout.PresentSrcKhr,
                    AccessFlags.TransferReadBit,
                    AccessFlags.MemoryReadBit
                );
            });

            WritePng(staging, memory, width, height, path);
        }
        finally
        {
            device.Api.DestroyImage(device.Handle, staging, null);
            device.Api.FreeMemory(device.Handle, memory, null);
        }
    }

    private (Silk.NET.Vulkan.Image Image, DeviceMemory Memory) CreateStagingImage(uint width, uint height)
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D { Width = width, Height = height, Depth = 1 },
            MipLevels = 1,
            ArrayLayers = 1,
            Format = swapchain.ImageFormat,
            Tiling = ImageTiling.Linear,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.TransferDstBit,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive
        };

        if (device.Api.CreateImage(device.Handle, in imageInfo, null, out var image) != Result.Success)
        {
            throw new InvalidOperationException("Failed to create the capture staging image.");
        }

        device.Api.GetImageMemoryRequirements(device.Handle, image, out var requirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = device.FindMemoryType(
                requirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
            )
        };

        if (device.Api.AllocateMemory(device.Handle, in allocInfo, null, out var memory) != Result.Success)
        {
            throw new InvalidOperationException("Failed to allocate capture staging memory.");
        }

        device.Api.BindImageMemory(device.Handle, image, memory, 0);

        return (image, memory);
    }

    private void Barrier(
        CommandBuffer commandBuffer,
        Silk.NET.Vulkan.Image image,
        ImageLayout oldLayout,
        ImageLayout newLayout,
        AccessFlags sourceAccess,
        AccessFlags destinationAccess)
    {
        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SrcAccessMask = sourceAccess,
            DstAccessMask = destinationAccess,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        device.Api.CmdPipelineBarrier(
            commandBuffer,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.TransferBit,
            0,
            0,
            null,
            0,
            null,
            1,
            in barrier
        );
    }

    private void WritePng(Silk.NET.Vulkan.Image image, DeviceMemory memory, uint width, uint height, string path)
    {
        ImageSubresource subresource = new() { AspectMask = ImageAspectFlags.ColorBit, MipLevel = 0, ArrayLayer = 0 };
        device.Api.GetImageSubresourceLayout(device.Handle, image, in subresource, out var layout);

        void* mapped;
        device.Api.MapMemory(device.Handle, memory, 0, Vk.WholeSize, 0, &mapped);

        try
        {
            var source = (byte*)mapped + layout.Offset;
            using var bitmap = new Image<Rgba32>((int)width, (int)height);

            // Swapchain formats are BGRA on virtually every desktop driver.
            var swapChannels = swapchain.ImageFormat is Format.B8G8R8A8Srgb or Format.B8G8R8A8Unorm;

            for (int y = 0; y < height; y++)
            {
                var row = source + y * (long)layout.RowPitch;

                for (int x = 0; x < width; x++)
                {
                    var pixel = row + x * 4;
                    bitmap[x, y] = swapChannels
                        ? new Rgba32(pixel[2], pixel[1], pixel[0], pixel[3])
                        : new Rgba32(pixel[0], pixel[1], pixel[2], pixel[3]);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            bitmap.SaveAsPng(path);

            Console.WriteLine($"[Cadmus] Frame captured to {path}");
        }
        finally
        {
            device.Api.UnmapMemory(device.Handle, memory);
        }
    }
}
