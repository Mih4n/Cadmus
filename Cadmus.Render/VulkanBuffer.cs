using Silk.NET.Vulkan;

namespace Cadmus.Render;

public unsafe class VulkanBuffer : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;

    public Silk.NET.Vulkan.Buffer Buffer { get; private set; }
    public DeviceMemory Memory { get; private set; }
    public ulong Size { get; }

    public VulkanBuffer(Vk vk, VulkanDevice device, ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties)
    {
        _vk = vk;
        _device = device;
        Size = size;

        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        if (_vk.CreateBuffer(_device.Device, in bufferInfo, null, out Silk.NET.Vulkan.Buffer buffer) != Result.Success)
        {
            throw new Exception("Failed to create buffer!");
        }
        Buffer = buffer;

        _vk.GetBufferMemoryRequirements(_device.Device, Buffer, out var memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (_vk.AllocateMemory(_device.Device, in allocInfo, null, out DeviceMemory memory) != Result.Success)
        {
            throw new Exception("Failed to allocate buffer memory!");
        }
        Memory = memory;

        _vk.BindBufferMemory(_device.Device, Buffer, Memory, 0);
    }

    public void* Map()
    {
        void* data;
        _vk.MapMemory(_device.Device, Memory, 0, Size, 0, &data);
        return data;
    }

    public void Unmap()
    {
        _vk.UnmapMemory(_device.Device, Memory);
    }

    public void CopyTo(VulkanBuffer dstBuffer, CommandPool commandPool, Queue queue)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1
        };

        _vk.AllocateCommandBuffers(_device.Device, in allocInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        _vk.BeginCommandBuffer(commandBuffer, in beginInfo);

        BufferCopy copyRegion = new()
        {
            Size = Size
        };

        _vk.CmdCopyBuffer(commandBuffer, Buffer, dstBuffer.Buffer, 1, in copyRegion);

        _vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        _vk.QueueSubmit(queue, 1, in submitInfo, default);
        _vk.QueueWaitIdle(queue);

        _vk.FreeCommandBuffers(_device.Device, commandPool, 1, in commandBuffer);
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
        _vk.DestroyBuffer(_device.Device, Buffer, null);
        _vk.FreeMemory(_device.Device, Memory, null);
    }
}
