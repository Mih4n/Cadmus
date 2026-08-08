using Cadmus.Engine.Geometry;
using Cadmus.Graphics.Vulkan;
using Cadmus.Graphics;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Resources;

/// <inheritdoc cref="IGpuResourceCache"/>
public sealed class GpuResourceCache : IGpuResourceCache, IDisposable
{
    private readonly VulkanDevice device;
    private readonly VulkanPipeline pipeline;
    private readonly VulkanOptions options;

    // Mesh does not override Equals, so the default comparer is reference identity — one upload per
    // Mesh instance, shared by every entity that points at it.
    private readonly Dictionary<Mesh, VulkanMesh> meshes = [];
    private readonly Dictionary<string, TextureEntry> textures = [];
    private readonly List<VulkanTexture> uploadedTextures = [];
    private readonly HashSet<string> reportedMissing = [];

    private TextureEntry? fallback;

    public GpuResourceCache(VulkanDevice device, VulkanPipeline pipeline, VulkanOptions options)
    {
        this.device = device;
        this.pipeline = pipeline;
        this.options = options;
    }

    public int TextureCount => uploadedTextures.Count;

    public int MeshCount => meshes.Count;

    public VulkanMesh GetMesh(Mesh mesh)
    {
        if (meshes.TryGetValue(mesh, out var uploaded))
        {
            return uploaded;
        }

        uploaded = new VulkanMesh(device, mesh);
        meshes[mesh] = uploaded;

        return uploaded;
    }

    public VulkanTexture GetTexture(string path) => GetEntry(path).Texture;

    public DescriptorSet GetTextureDescriptor(string path) => GetEntry(path).Descriptor;

    private TextureEntry GetEntry(string path)
    {
        if (textures.TryGetValue(path, out var entry))
        {
            return entry;
        }

        var fullPath = VulkanOptions.ResolvePath(path);

        try
        {
            entry = Upload(fullPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SixLabors.ImageSharp.UnknownImageFormatException or SixLabors.ImageSharp.InvalidImageContentException)
        {
            if (reportedMissing.Add(path))
            {
                Console.WriteLine($"[Cadmus] Texture '{path}' could not be loaded ({exception.Message}); using the fallback.");
            }

            entry = GetFallback();
        }

        textures[path] = entry;

        return entry;
    }

    private TextureEntry GetFallback()
    {
        if (fallback is not null)
        {
            return fallback;
        }

        var fallbackPath = VulkanOptions.ResolvePath(options.FallbackTexturePath);
        fallback = Upload(fallbackPath);

        return fallback;
    }

    private TextureEntry Upload(string fullPath)
    {
        var texture = new VulkanTexture(device, fullPath);
        uploadedTextures.Add(texture);

        var descriptor = pipeline.AllocateTextureSet();
        pipeline.UpdateTextureSet(descriptor, texture);

        return new TextureEntry(texture, descriptor);
    }

    public void Dispose()
    {
        device.WaitIdle();

        foreach (var mesh in meshes.Values)
        {
            mesh.Dispose();
        }
        meshes.Clear();

        // Tracked separately from the path map because several paths may share the fallback entry.
        foreach (var texture in uploadedTextures)
        {
            texture.Dispose();
        }

        uploadedTextures.Clear();
        textures.Clear();
        fallback = null;
    }

    private sealed record TextureEntry(VulkanTexture Texture, DescriptorSet Descriptor);
}
