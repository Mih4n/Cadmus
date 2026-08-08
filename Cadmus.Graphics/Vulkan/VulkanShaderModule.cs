using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

public sealed unsafe class VulkanShaderModule : IDisposable
{
    private readonly VulkanDevice device;

    public ShaderModule Handle { get; }

    public VulkanShaderModule(VulkanDevice device, byte[] spirv)
    {
        this.device = device;

        if (spirv.Length == 0 || spirv.Length % 4 != 0)
        {
            throw new InvalidDataException($"SPIR-V payload of {spirv.Length} bytes is not a multiple of 4.");
        }

        fixed (byte* pCode = spirv)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode = (uint*)pCode
            };

            if (device.Api.CreateShaderModule(device.Handle, in createInfo, null, out var module) != Result.Success)
            {
                throw new InvalidOperationException("Failed to create a shader module.");
            }

            Handle = module;
        }
    }

    public static VulkanShaderModule FromFile(VulkanDevice device, string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Compiled shader not found: {path}", path);
        }

        return new VulkanShaderModule(device, File.ReadAllBytes(path));
    }

    public void Dispose() => device.Api.DestroyShaderModule(device.Handle, Handle, null);
}
