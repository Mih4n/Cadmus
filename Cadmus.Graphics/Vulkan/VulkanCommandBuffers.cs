using Cadmus.Graphics.Vulkan;
using Cadmus.Graphics;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

/// <summary>One primary command buffer per frame in flight.</summary>
public sealed unsafe class VulkanCommandBuffers : IDisposable
{
    private readonly VulkanDevice device;

    public CommandBuffer[] Buffers { get; }

    public VulkanCommandBuffers(VulkanDevice device, VulkanOptions options)
    {
        this.device = device;

        var count = Math.Max(1, options.FramesInFlight);
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
            if (device.Api.AllocateCommandBuffers(device.Handle, in allocInfo, pBuffers) != Result.Success)
            {
                throw new InvalidOperationException("Failed to allocate command buffers.");
            }
        }
    }

    public void Dispose()
    {
        fixed (CommandBuffer* pBuffers = Buffers)
        {
            device.Api.FreeCommandBuffers(device.Handle, device.CommandPool, (uint)Buffers.Length, pBuffers);
        }
    }
}
