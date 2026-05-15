using Silk.NET.Vulkan;

namespace Cadmus.Render;

public unsafe class VulkanShaderModule : IDisposable
{
    private readonly Vk _vk;
    private readonly Device _device;

    public ShaderModule Module { get; }

    public VulkanShaderModule(Vk vk, Device device, byte[] code)
    {
        _vk = vk;
        _device = device;

        fixed (byte* pCode = code)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
                PCode = (uint*)pCode
            };

            if (_vk.CreateShaderModule(_device, in createInfo, null, out ShaderModule module) != Result.Success)
            {
                throw new Exception("Failed to create shader module!");
            }
            Module = module;
            {
                throw new Exception("Failed to create shader module!");
            }
        }
    }

    public static VulkanShaderModule FromFile(Vk vk, Device device, string path)
    {
        var code = File.ReadAllBytes(path);
        return new VulkanShaderModule(vk, device, code);
    }

    public void Dispose()
    {
        _vk.DestroyShaderModule(_device, Module, null);
    }
}
