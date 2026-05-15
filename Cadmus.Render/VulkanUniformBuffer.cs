using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace Cadmus.Render;

[StructLayout(LayoutKind.Sequential)]
public struct UniformBufferObject
{
    public Matrix4x4 ViewProj;
    public Matrix4x4 Model;
}

public unsafe class VulkanUniformBuffer : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;

    public VulkanBuffer Buffer { get; }
    public ulong Size { get; }

    public VulkanUniformBuffer(Vk vk, VulkanDevice device)
    {
        _vk = vk;
        _device = device;
        Size = (ulong)sizeof(UniformBufferObject);

        Buffer = new VulkanBuffer(vk, device, Size,
            BufferUsageFlags.UniformBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
    }

    public void Update(UniformBufferObject ubo)
    {
        void* data = Buffer.Map();
        *(UniformBufferObject*)data = ubo;
        Buffer.Unmap();
    }

    public void Dispose()
    {
        Buffer.Dispose();
    }
}
