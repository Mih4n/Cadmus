using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

public sealed unsafe class VulkanBuffer : IDisposable
{
    private readonly VulkanDevice device;
    private void* mapped;

    public Silk.NET.Vulkan.Buffer Handle { get; }
    public DeviceMemory Memory { get; }
    public ulong Size { get; }

    public VulkanBuffer(VulkanDevice device, ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties)
    {
        this.device = device;
        Size = size;

        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        if (device.Api.CreateBuffer(device.Handle, in bufferInfo, null, out var buffer) != Result.Success)
        {
            throw new InvalidOperationException("Failed to create a buffer.");
        }
        Handle = buffer;

        device.Api.GetBufferMemoryRequirements(device.Handle, Handle, out var requirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = device.FindMemoryType(requirements.MemoryTypeBits, properties)
        };

        if (device.Api.AllocateMemory(device.Handle, in allocInfo, null, out var memory) != Result.Success)
        {
            throw new InvalidOperationException("Failed to allocate buffer memory.");
        }
        Memory = memory;

        device.Api.BindBufferMemory(device.Handle, Handle, Memory, 0);
    }

    public void* Map()
    {
        if (mapped is not null)
        {
            return mapped;
        }

        void* data;
        device.Api.MapMemory(device.Handle, Memory, 0, Size, 0, &data);
        mapped = data;

        return mapped;
    }

    public void Unmap()
    {
        if (mapped is null)
        {
            return;
        }

        device.Api.UnmapMemory(device.Handle, Memory);
        mapped = null;
    }

    public void Write<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        var byteCount = (ulong)(data.Length * sizeof(T));
        var destination = Map();

        fixed (T* source = data)
        {
            System.Buffer.MemoryCopy(source, destination, Size, byteCount);
        }

        Unmap();
    }

    public void CopyTo(VulkanBuffer destination) => device.SubmitImmediate(commandBuffer =>
    {
        BufferCopy region = new() { Size = Size };
        device.Api.CmdCopyBuffer(commandBuffer, Handle, destination.Handle, 1, in region);
    });

    public void Dispose()
    {
        Unmap();
        device.Api.DestroyBuffer(device.Handle, Handle, null);
        device.Api.FreeMemory(device.Handle, Memory, null);
    }
}
