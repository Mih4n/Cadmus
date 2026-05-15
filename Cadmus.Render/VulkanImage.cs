using Silk.NET.Vulkan;

namespace Cadmus.Render;

public unsafe class VulkanImage : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;

    public Image Image { get; private set; }
    public DeviceMemory ImageMemory { get; private set; }
    public ImageView ImageView { get; private set; }
    public uint Width { get; }
    public uint Height { get; }

    public VulkanImage(Vk vk, VulkanDevice device, uint width, uint height, Format format, ImageTiling tiling,
        ImageUsageFlags usage, MemoryPropertyFlags properties, ImageAspectFlags aspectFlags, SampleCountFlags samples = SampleCountFlags.Count1Bit)
    {
        _vk = vk;
        _device = device;
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

        if (_vk.CreateImage(_device.Device, in imageInfo, null, out Image image) != Result.Success)
        {
            throw new Exception("Failed to create image!");
        }
        Image = image;

        _vk.GetImageMemoryRequirements(_device.Device, Image, out var memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (_vk.AllocateMemory(_device.Device, in allocInfo, null, out DeviceMemory imageMemory) != Result.Success)
        {
            throw new Exception("Failed to allocate image memory!");
        }
        ImageMemory = imageMemory;

        _vk.BindImageMemory(_device.Device, Image, ImageMemory, 0);

        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Image,
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

        if (_vk.CreateImageView(_device.Device, in viewInfo, null, out ImageView imageView) != Result.Success)
        {
            throw new Exception("Failed to create image view!");
        }
        ImageView = imageView;
    }

    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_device.PhysicalDevice, out var memProperties);

        for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 && (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }

        throw new Exception("Failed to find suitable memory type!");
    }

    public void Dispose()
    {
        _vk.DestroyImageView(_device.Device, ImageView, null);
        _vk.DestroyImage(_device.Device, Image, null);
        _vk.FreeMemory(_device.Device, ImageMemory, null);
    }
}
