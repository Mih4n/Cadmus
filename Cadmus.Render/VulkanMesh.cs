using System.Numerics;
using System.Runtime.InteropServices;
using Cadmus.Domain.Rendering;
using Silk.NET.Vulkan;

namespace Cadmus.Render;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position;
    public Vector2 UV;

    public Vertex(Vector3 position, Vector2 uv)
    {
        Position = position;
        UV = uv;
    }

    public static VertexInputBindingDescription GetBindingDescription()
    {
        return new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint)Marshal.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex
        };
    }

    public static VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        return new[]
        {
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Position))
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(UV))
            }
        };
    }
}

public unsafe class VulkanMesh : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;

    public VulkanBuffer VertexBuffer { get; }
    public VulkanBuffer IndexBuffer { get; }
    public uint IndexCount { get; }

    public VulkanMesh(Vk vk, VulkanDevice device, Mesh mesh)
    {
        _vk = vk;
        _device = device;
        IndexCount = (uint)mesh.Indices.Length;

        var vertices = new Vertex[mesh.Positions.Length];
        for (int i = 0; i < mesh.Positions.Length; i++)
        {
            vertices[i] = new Vertex(mesh.Positions[i], mesh.UVs[i]);
        }

        ulong vertexBufferSize = (ulong)(vertices.Length * sizeof(Vertex));
        ulong indexBufferSize = (ulong)(mesh.Indices.Length * sizeof(ushort));

        // Staging buffers
        var stagingVertex = new VulkanBuffer(vk, device, vertexBufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var stagingIndex = new VulkanBuffer(vk, device, indexBufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        // Copy data to staging
        fixed (Vertex* pVertices = vertices)
        {
            void* data = stagingVertex.Map();
            System.Buffer.MemoryCopy(pVertices, data, vertexBufferSize, vertexBufferSize);
            stagingVertex.Unmap();
        }

        fixed (ushort* pIndices = mesh.Indices)
        {
            void* data = stagingIndex.Map();
            System.Buffer.MemoryCopy(pIndices, data, indexBufferSize, indexBufferSize);
            stagingIndex.Unmap();
        }

        // Device local buffers
        VertexBuffer = new VulkanBuffer(vk, device, vertexBufferSize,
            BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit);

        IndexBuffer = new VulkanBuffer(vk, device, indexBufferSize,
            BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit);

        stagingVertex.CopyTo(VertexBuffer, device.CommandPool, device.GraphicsQueue);
        stagingIndex.CopyTo(IndexBuffer, device.CommandPool, device.GraphicsQueue);

        stagingVertex.Dispose();
        stagingIndex.Dispose();
    }

    public void Dispose()
    {
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}
