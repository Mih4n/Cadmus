using Silk.NET.Core;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Cadmus.Render;

public unsafe class VulkanSwapchain : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;
    private readonly IWindow _window;
    private readonly SurfaceKHR _surface;
    private readonly KhrSurface _khrSurface;
    private readonly KhrSwapchain _khrSwapchain;

    public SwapchainKHR Swapchain { get; private set; }
    public Image[] SwapchainImages { get; private set; } = [];
    public ImageView[] SwapchainImageViews { get; private set; } = [];
    public Framebuffer[] Framebuffers { get; private set; } = [];
    public Format ImageFormat { get; private set; }
    public Extent2D Extent { get; private set; }
    public uint ImageCount => (uint)SwapchainImages.Length;

    public VulkanSwapchain(Vk vk, VulkanDevice device, IWindow window, SurfaceKHR surface, KhrSurface khrSurface, KhrSwapchain khrSwapchain)
    {
        _vk = vk;
        _device = device;
        _window = window;
        _surface = surface;
        _khrSurface = khrSurface;
        _khrSwapchain = khrSwapchain;

        CreateSwapchain();
        CreateImageViews();
    }

    public void CreateFramebuffers(RenderPass renderPass, ImageView depthImageView)
    {
        Framebuffers = new Framebuffer[SwapchainImageViews.Length];
        var attachments = stackalloc ImageView[2];

        for (int i = 0; i < SwapchainImageViews.Length; i++)
        {
            attachments[0] = SwapchainImageViews[i];
            attachments[1] = depthImageView;

            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = renderPass,
                AttachmentCount = 2,
                PAttachments = attachments,
                Width = Extent.Width,
                Height = Extent.Height,
                Layers = 1
            };

            if (_vk.CreateFramebuffer(_device.Device, in framebufferInfo, null, out Framebuffers[i]) != Result.Success)
            {
                throw new Exception("Failed to create framebuffer!");
            }
        }
    }

    public void Recreate(RenderPass renderPass, ImageView depthImageView)
    {
        CleanupFramebuffers();
        CleanupSwapchain();
        CreateSwapchain();
        CreateImageViews();
        CreateFramebuffers(renderPass, depthImageView);
    }

    public void CreateSwapchain()
    {
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(_device.PhysicalDevice, _surface, out var surfaceCapabilities);
        var surfaceFormat = ChooseSwapSurfaceFormat();
        var presentMode = ChooseSwapPresentMode();
        var extent = ChooseSwapExtent(surfaceCapabilities);

        uint imageCount = surfaceCapabilities.MinImageCount + 1;
        if (surfaceCapabilities.MaxImageCount > 0 && imageCount > surfaceCapabilities.MaxImageCount)
        {
            imageCount = surfaceCapabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR createInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            PreTransform = surfaceCapabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default
        };

        var queueFamilyIndices = stackalloc[] { _device.GraphicsFamily, _device.PresentFamily };
        if (_device.GraphicsFamily != _device.PresentFamily)
        {
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = queueFamilyIndices;
        }
        else
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        if (_khrSwapchain.CreateSwapchain(_device.Device, &createInfo, null, out SwapchainKHR swapchain) != Result.Success)
        {
            throw new Exception("Failed to create swap chain!");
        }

        Swapchain = swapchain;

        _khrSwapchain.GetSwapchainImages(_device.Device, Swapchain, &imageCount, null);
        SwapchainImages = new Image[imageCount];
        fixed (Image* pImages = SwapchainImages)
        {
            _khrSwapchain.GetSwapchainImages(_device.Device, Swapchain, &imageCount, pImages);
        }

        ImageFormat = surfaceFormat.Format;
        Extent = extent;
    }

    public void CreateImageViews()
    {
        SwapchainImageViews = new ImageView[SwapchainImages.Length];

        for (int i = 0; i < SwapchainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = SwapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = ImageFormat,
                Components =
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity
                },
                SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };

            if (_vk.CreateImageView(_device.Device, in createInfo, null, out SwapchainImageViews[i]) != Result.Success)
            {
                throw new Exception("Failed to create image views!");
            }
        }
    }

    private SurfaceFormatKHR ChooseSwapSurfaceFormat()
    {
        uint formatCount = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(_device.PhysicalDevice, _surface, &formatCount, null);
        var formats = stackalloc SurfaceFormatKHR[(int)formatCount];
        _khrSurface.GetPhysicalDeviceSurfaceFormats(_device.PhysicalDevice, _surface, &formatCount, formats);

        for (int i = 0; i < formatCount; i++)
        {
            if (formats[i].Format == Format.B8G8R8A8Unorm && formats[i].ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                return formats[i];
            }
        }

        return formats[0];
    }

    private PresentModeKHR ChooseSwapPresentMode()
    {
        uint presentModeCount = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(_device.PhysicalDevice, _surface, &presentModeCount, null);
        var presentModes = stackalloc PresentModeKHR[(int)presentModeCount];
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(_device.PhysicalDevice, _surface, &presentModeCount, presentModes);

        for (int i = 0; i < presentModeCount; i++)
        {
            if (presentModes[i] == PresentModeKHR.MailboxKhr)
            {
                return presentModes[i];
            }
        }

        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }

        var framebufferSize = _window.FramebufferSize;
        Extent2D actualExtent = new()
        {
            Width = (uint)framebufferSize.X,
            Height = (uint)framebufferSize.Y
        };

        actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
        actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

        return actualExtent;
    }

    public void CleanupFramebuffers()
    {
        if (Framebuffers == null) return;
        foreach (var framebuffer in Framebuffers)
        {
            _vk.DestroyFramebuffer(_device.Device, framebuffer, null);
        }
        Framebuffers = [];
    }

    public void CleanupSwapchain()
    {
        foreach (var imageView in SwapchainImageViews)
        {
            _vk.DestroyImageView(_device.Device, imageView, null);
        }
        _khrSwapchain.DestroySwapchain(_device.Device, Swapchain, null);
    }

    public void Dispose()
    {
        CleanupFramebuffers();
        CleanupSwapchain();
    }
}
