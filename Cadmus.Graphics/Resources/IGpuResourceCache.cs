using Cadmus.Engine.Geometry;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Resources;

/// <summary>
/// Owns every GPU-side asset and hands out the descriptor set that binds it. Systems inject this
/// instead of uploading textures themselves.
/// </summary>
public interface IGpuResourceCache
{
    /// <summary>Uploads the mesh on first use and returns the cached buffers.</summary>
    VulkanMesh GetMesh(Mesh mesh);

    /// <summary>
    /// Uploads the texture at <paramref name="path"/> (relative to the app directory) on first use.
    /// Missing or unreadable files fall back to the built-in placeholder texture.
    /// </summary>
    VulkanTexture GetTexture(string path);

    /// <summary>The descriptor set of set 1 for this texture, allocated once per texture.</summary>
    DescriptorSet GetTextureDescriptor(string path);

    /// <summary>How many distinct textures are resident.</summary>
    int TextureCount { get; }

    /// <summary>How many distinct meshes are resident.</summary>
    int MeshCount { get; }
}
