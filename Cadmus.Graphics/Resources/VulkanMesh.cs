using System.Numerics;
using System.Runtime.InteropServices;
using Cadmus.Engine.Geometry;
using Cadmus.Graphics.Vulkan;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Resources;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex(Vector3 position, Vector2 uv)
{
    public Vector3 Position = position;
    public Vector2 UV = uv;

    public static VertexInputBindingDescription GetBindingDescription() => new()
    {
        Binding = 0,
        Stride = (uint)Marshal.SizeOf<Vertex>(),
        InputRate = VertexInputRate.Vertex
    };

    public static VertexInputAttributeDescription[] GetAttributeDescriptions() =>
    [
        new()
        {
            Binding = 0,
            Location = 0,
            Format = Format.R32G32B32Sfloat,
            Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Position))
        },
        new()
        {
            Binding = 0,
            Location = 1,
            Format = Format.R32G32Sfloat,
            Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(UV))
        }
    ];
}

/// <summary>Device-local vertex/index buffers uploaded from a domain <see cref="Mesh"/>.</summary>
public sealed unsafe class VulkanMesh : IDisposable
{
    public VulkanBuffer VertexBuffer { get; }
    public VulkanBuffer IndexBuffer { get; }
    public uint IndexCount { get; }

    public VulkanMesh(VulkanDevice device, Mesh mesh)
    {
        if (mesh.Positions.Length != mesh.UVs.Length)
        {
            throw new ArgumentException("Mesh positions and UVs must have the same length.", nameof(mesh));
        }

        IndexCount = (uint)mesh.Indices.Length;

        var vertices = new Vertex[mesh.Positions.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vertex(mesh.Positions[i], mesh.UVs[i]);
        }

        var vertexSize = (ulong)(vertices.Length * sizeof(Vertex));
        var indexSize = (ulong)(mesh.Indices.Length * sizeof(ushort));

        using var vertexStaging = new VulkanBuffer(
            device,
            vertexSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );
        vertexStaging.Write<Vertex>(vertices);

        using var indexStaging = new VulkanBuffer(
            device,
            indexSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );
        indexStaging.Write<ushort>(mesh.Indices);

        VertexBuffer = new VulkanBuffer(
            device,
            vertexSize,
            BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit
        );

        IndexBuffer = new VulkanBuffer(
            device,
            indexSize,
            BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit
        );

        vertexStaging.CopyTo(VertexBuffer);
        indexStaging.CopyTo(IndexBuffer);
    }

    public void Dispose()
    {
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}
