using Cadmus.Graphics.Vulkan;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics;

/// <summary>
/// One persistently mapped uniform buffer per frame in flight, sliced into per-draw slots addressed
/// through a dynamic descriptor offset. This is what lets every sprite carry its own model matrix
/// inside a single command buffer — writing one shared uniform per draw would make all draws use
/// the last matrix written.
/// </summary>
public sealed unsafe class UniformRing : IDisposable
{
    private readonly VulkanBuffer[] buffers;
    private readonly DescriptorSet[] sets;
    private readonly int capacity;
    private int used;
    private int frame;

    /// <summary>Slot size, padded up to the device's minimum dynamic-offset alignment.</summary>
    public uint Stride { get; }

    public UniformRing(VulkanDevice device, VulkanPipeline pipeline, VulkanOptions options)
    {
        capacity = Math.Max(1, options.MaxDrawsPerFrame);

        var alignment = device.Properties.Limits.MinUniformBufferOffsetAlignment;
        var size = (ulong)sizeof(ObjectUniforms);
        Stride = (uint)(alignment == 0 ? size : (size + alignment - 1) / alignment * alignment);

        var frames = (int)Math.Max(1, options.FramesInFlight);
        buffers = new VulkanBuffer[frames];
        sets = new DescriptorSet[frames];

        for (int i = 0; i < frames; i++)
        {
            buffers[i] = new VulkanBuffer(
                device,
                (ulong)Stride * (ulong)capacity,
                BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            buffers[i].Map();

            sets[i] = pipeline.AllocateUniformSet();
            pipeline.UpdateUniformSet(sets[i], buffers[i], (ulong)sizeof(ObjectUniforms));
        }
    }

    /// <summary>Descriptor set for the frame currently being recorded.</summary>
    public DescriptorSet CurrentSet => sets[frame];

    /// <summary>Starts recording into the given frame's buffer.</summary>
    public void BeginFrame(uint frameIndex)
    {
        frame = (int)(frameIndex % (uint)buffers.Length);
        used = 0;
    }

    /// <summary>
    /// Writes one draw's uniforms and returns the dynamic offset to bind with. Returns false when
    /// the frame's slot budget is exhausted.
    /// </summary>
    public bool TryPush(in ObjectUniforms uniforms, out uint dynamicOffset)
    {
        if (used >= capacity)
        {
            dynamicOffset = 0;
            return false;
        }

        var offset = (uint)(used * Stride);
        var destination = (byte*)buffers[frame].Map() + offset;
        *(ObjectUniforms*)destination = uniforms;

        used++;
        dynamicOffset = offset;

        return true;
    }

    public void Dispose()
    {
        foreach (var buffer in buffers)
        {
            buffer.Dispose();
        }
    }
}
