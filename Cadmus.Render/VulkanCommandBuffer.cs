using Silk.NET.Vulkan;

namespace Cadmus.Render;

public unsafe class VulkanCommandBuffers : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;

    public CommandBuffer[] Buffers { get; private set; }

    public VulkanCommandBuffers(Vk vk, VulkanDevice device, uint count)
    {
        _vk = vk;
        _device = device;
        Buffers = new CommandBuffer[count];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = device.CommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = count
        };

        fixed (CommandBuffer* pBuffers = Buffers)
        {
            if (_vk.AllocateCommandBuffers(device.Device, in allocInfo, pBuffers) != Result.Success)
            {
                throw new Exception("Failed to allocate command buffers!");
            }
        }
    }

    public void Dispose()
    {
        fixed (CommandBuffer* pBuffers = Buffers)
        {
            _vk.FreeCommandBuffers(_device.Device, _device.CommandPool, (uint)Buffers.Length, pBuffers);
        }
    }
}
