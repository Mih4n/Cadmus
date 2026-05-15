using Silk.NET.Vulkan;

namespace Cadmus.Render;

public unsafe class VulkanFrameSync : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;
    private readonly uint _frameCount;

    public Silk.NET.Vulkan.Semaphore[] ImageAvailableSemaphores { get; }
    public Silk.NET.Vulkan.Semaphore[] RenderFinishedSemaphores { get; }
    public Fence[] InFlightFences { get; }

    public VulkanFrameSync(Vk vk, VulkanDevice device, uint frameCount = 2)
    {
        _vk = vk;
        _device = device;
        _frameCount = frameCount;

        ImageAvailableSemaphores = new Silk.NET.Vulkan.Semaphore[frameCount];
        RenderFinishedSemaphores = new Silk.NET.Vulkan.Semaphore[frameCount];
        InFlightFences = new Fence[frameCount];

        SemaphoreCreateInfo semaphoreInfo = new() { SType = StructureType.SemaphoreCreateInfo };
        FenceCreateInfo fenceInfo = new() { SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit };

        for (int i = 0; i < frameCount; i++)
        {
            if (_vk.CreateSemaphore(_device.Device, in semaphoreInfo, null, out Silk.NET.Vulkan.Semaphore imageAvailable) != Result.Success ||
                _vk.CreateSemaphore(_device.Device, in semaphoreInfo, null, out Silk.NET.Vulkan.Semaphore renderFinished) != Result.Success ||
                _vk.CreateFence(_device.Device, in fenceInfo, null, out Fence inFlight) != Result.Success)
            {
                throw new Exception("Failed to create synchronization objects!");
            }

            ImageAvailableSemaphores[i] = imageAvailable;
            RenderFinishedSemaphores[i] = renderFinished;
            InFlightFences[i] = inFlight;
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < _frameCount; i++)
        {
            _vk.DestroySemaphore(_device.Device, ImageAvailableSemaphores[i], null);
            _vk.DestroySemaphore(_device.Device, RenderFinishedSemaphores[i], null);
            _vk.DestroyFence(_device.Device, InFlightFences[i], null);
        }
    }
}
