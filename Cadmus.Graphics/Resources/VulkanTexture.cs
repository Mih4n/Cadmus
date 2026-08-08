using Cadmus.Graphics.Vulkan;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Cadmus.Graphics.Resources;

public sealed unsafe class VulkanTexture : IDisposable
{
    private readonly VulkanDevice device;

    public VulkanImage Image { get; }
    public Sampler Sampler { get; }
    public uint Width => Image.Width;
    public uint Height => Image.Height;

    public VulkanTexture(VulkanDevice device, string path)
    {
        this.device = device;

        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);

        var width = (uint)image.Width;
        var height = (uint)image.Height;
        var pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);

        using var staging = new VulkanBuffer(
            device,
            (ulong)(width * height * 4),
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );
        staging.Write<Rgba32>(pixels);

        Image = new VulkanImage(
            device,
            width,
            height,
            Format.R8G8B8A8Srgb,
            ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ImageAspectFlags.ColorBit
        );

        Image.TransitionLayout(ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        Image.CopyFrom(staging);
        Image.TransitionLayout(ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        Sampler = CreateSampler();
    }

    private Sampler CreateSampler()
    {
        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Nearest,
            MinFilter = Filter.Nearest,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            // Requesting anisotropy without the device feature enabled is a validation error.
            AnisotropyEnable = device.SupportsAnisotropy,
            MaxAnisotropy = device.SupportsAnisotropy ? device.Properties.Limits.MaxSamplerAnisotropy : 1f,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Nearest,
            MipLodBias = 0,
            MinLod = 0,
            MaxLod = 0
        };

        if (device.Api.CreateSampler(device.Handle, in samplerInfo, null, out var sampler) != Result.Success)
        {
            throw new InvalidOperationException("Failed to create a texture sampler.");
        }

        return sampler;
    }

    public void Dispose()
    {
        device.Api.DestroySampler(device.Handle, Sampler, null);
        Image.Dispose();
    }
}
