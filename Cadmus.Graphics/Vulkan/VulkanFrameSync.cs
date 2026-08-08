using Cadmus.Graphics.Vulkan;
using Cadmus.Graphics;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

/// <summary>
/// Frame synchronisation primitives. "Image available" fences/semaphores are per frame-in-flight,
/// while "render finished" semaphores are per swapchain image — signalling a semaphore that a
/// previous present may still be waiting on is a validation error otherwise.
/// </summary>
public sealed unsafe class VulkanFrameSync : IDisposable
{
    private readonly VulkanDevice device;

    public uint FramesInFlight { get; }
    public Silk.NET.Vulkan.Semaphore[] ImageAvailable { get; }
    public Silk.NET.Vulkan.Semaphore[] RenderFinished { get; }
    public Fence[] InFlight { get; }

    public VulkanFrameSync(VulkanDevice device, VulkanSwapchain swapchain, VulkanOptions options)
    {
        this.device = device;
        FramesInFlight = Math.Max(1, options.FramesInFlight);

        ImageAvailable = new Silk.NET.Vulkan.Semaphore[FramesInFlight];
        InFlight = new Fence[FramesInFlight];
        RenderFinished = new Silk.NET.Vulkan.Semaphore[swapchain.ImageCount];

        SemaphoreCreateInfo semaphoreInfo = new() { SType = StructureType.SemaphoreCreateInfo };
        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (int i = 0; i < FramesInFlight; i++)
        {
            if (device.Api.CreateSemaphore(device.Handle, in semaphoreInfo, null, out ImageAvailable[i]) != Result.Success ||
                device.Api.CreateFence(device.Handle, in fenceInfo, null, out InFlight[i]) != Result.Success)
            {
                throw new InvalidOperationException("Failed to create frame synchronisation objects.");
            }
        }

        for (int i = 0; i < RenderFinished.Length; i++)
        {
            if (device.Api.CreateSemaphore(device.Handle, in semaphoreInfo, null, out RenderFinished[i]) != Result.Success)
            {
                throw new InvalidOperationException("Failed to create a render-finished semaphore.");
            }
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < FramesInFlight; i++)
        {
            device.Api.DestroySemaphore(device.Handle, ImageAvailable[i], null);
            device.Api.DestroyFence(device.Handle, InFlight[i], null);
        }

        foreach (var semaphore in RenderFinished)
        {
            device.Api.DestroySemaphore(device.Handle, semaphore, null);
        }
    }
}
