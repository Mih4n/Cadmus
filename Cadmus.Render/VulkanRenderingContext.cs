using System.Runtime.InteropServices;
using Cadmus.Domain.Contracts.Components;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Cadmus.Render;

public unsafe class VulkanRenderingContext : IComponent, IDisposable
{
    public Vk Vulkan = null!;
    public IWindow Window = null!;
    public Instance Instance;
    public SurfaceKHR Surface;

    public VulkanDevice? Device { get; private set; }
    public VulkanSwapchain? Swapchain { get; private set; }
    public VulkanRenderPass? RenderPass { get; private set; }
    public VulkanDepthBuffer? DepthBuffer { get; private set; }
    public VulkanFrameSync? FrameSync { get; private set; }
    public VulkanCommandBuffers? CommandBuffers { get; private set; }

    public KhrSurface? KhrSurface { get; private set; }
    public KhrSwapchain? KhrSwapchain { get; private set; }

    public uint CurrentFrame { get; private set; }
    public bool FramebufferResized { get; set; }

    public VulkanRenderingContext()
    {
        InitWindow();
        InitVulkan();
        CreateSurface();
        InitDeviceAndSwapchain();
    }

    public void InitWindow()
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "Cadmus"
        };

        Window = Silk.NET.Windowing.Window.Create(options);
        Window.Initialize();

        if (Window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        Window.FramebufferResize += size => FramebufferResized = true;
    }

    public void InitVulkan()
    {
        Vulkan = Vk.GetApi();
        CreateInstance();
    }

    private void CreateInstance()
    {
        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Cadmus"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("Cadmus"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        if (Window.VkSurface is null) throw new Exception("No vulkan api");

        var glfwExtensions = Window.VkSurface.GetRequiredExtensions(out var glfwExtensionCount);

        createInfo.EnabledExtensionCount = glfwExtensionCount;
        createInfo.PpEnabledExtensionNames = glfwExtensions;
        createInfo.EnabledLayerCount = 0;

        if (Vulkan.CreateInstance(in createInfo, null, out Instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }

        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);
    }

    private void CreateSurface()
    {
        if (Window.VkSurface is null) throw new Exception("Windowing platform doesn't support Vulkan.");

        unsafe
        {
            var surfaceHandle = Window.VkSurface.Create<VkHandle>(new VkHandle(Instance.Handle), null);
            Surface = new SurfaceKHR(surfaceHandle.Handle);
        }
    }

    private void InitDeviceAndSwapchain()
    {
        if (!Vulkan.TryGetInstanceExtension(Instance, out KhrSurface khrSurface))
        {
            throw new Exception("Failed to get KHR_surface extension.");
        }
        KhrSurface = khrSurface;

        Device = new VulkanDevice(Vulkan, Instance, Surface, KhrSurface);

        if (!Vulkan.TryGetDeviceExtension(Instance, Device.Device, out KhrSwapchain khrSwapchain))
        {
            throw new Exception("Failed to get KHR_swapchain extension.");
        }
        KhrSwapchain = khrSwapchain;

        Swapchain = new VulkanSwapchain(Vulkan, Device, Window, Surface, KhrSurface, KhrSwapchain);
        RenderPass = new VulkanRenderPass(Vulkan, Device, Swapchain.ImageFormat);
        DepthBuffer = new VulkanDepthBuffer(Vulkan, Device, Swapchain.Extent.Width, Swapchain.Extent.Height);
        Swapchain.CreateFramebuffers(RenderPass.RenderPass, DepthBuffer.Image.ImageView);
        FrameSync = new VulkanFrameSync(Vulkan, Device, 2);
        CommandBuffers = new VulkanCommandBuffers(Vulkan, Device, 2);
    }

    public void RecreateSwapchain()
    {
        if (Window.FramebufferSize.X == 0 || Window.FramebufferSize.Y == 0)
        {
            return;
        }

        Vulkan.DeviceWaitIdle(Device!.Device);

        DepthBuffer?.Dispose();
        Swapchain?.CleanupFramebuffers();
        Swapchain?.CleanupSwapchain();

        Swapchain?.CreateSwapchain();
        Swapchain?.CreateImageViews();

        DepthBuffer = new VulkanDepthBuffer(Vulkan, Device!, Swapchain!.Extent.Width, Swapchain.Extent.Height);
        Swapchain.CreateFramebuffers(RenderPass!.RenderPass, DepthBuffer.Image.ImageView);
    }

    public void Dispose()
    {
        CommandBuffers?.Dispose();
        FrameSync?.Dispose();
        DepthBuffer?.Dispose();
        Swapchain?.Dispose();
        RenderPass?.Dispose();
        Device?.Dispose();
        KhrSurface?.DestroySurface(Instance, Surface, null);
        Vulkan.DestroyInstance(Instance, null);
        Window.Dispose();
        Vulkan.Dispose();
    }
}
