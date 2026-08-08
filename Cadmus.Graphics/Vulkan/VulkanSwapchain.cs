using Cadmus.Core.Windowing;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

/// <summary>
/// The swapchain and its image views. Recreation is driven by the renderer (out-of-date/resize) and
/// announced through <see cref="Recreated"/> so dependent resources can rebuild.
/// </summary>
public sealed unsafe class VulkanSwapchain : IDisposable
{
    private readonly VulkanInstance instance;
    private readonly VulkanDevice device;
    private readonly IGameWindow window;
    private readonly KhrSwapchain swapchainApi;
    private readonly Vk vk;

    private bool disposed;

    public SwapchainKHR Handle { get; private set; }
    public Image[] Images { get; private set; } = [];
    public ImageView[] ImageViews { get; private set; } = [];
    public Format ImageFormat { get; private set; }
    public Extent2D Extent { get; private set; }
    public uint ImageCount => (uint)Images.Length;

    /// <summary>True when the surface allows presented images to be used as a transfer source.</summary>
    public bool SupportsCapture { get; private set; }

    /// <summary>Raised after the swapchain has been rebuilt with a new extent.</summary>
    public event Action? Recreated;

    public VulkanSwapchain(VulkanInstance instance, VulkanDevice device, IGameWindow window)
    {
        this.instance = instance;
        this.device = device;
        this.window = window;
        vk = device.Api;
        swapchainApi = device.SwapchainApi;

        Create();
    }

    public void Recreate()
    {
        device.WaitIdle();

        DestroyImageViews();
        swapchainApi.DestroySwapchain(device.Handle, Handle, null);

        Create();

        Recreated?.Invoke();
    }

    private void Create()
    {
        instance.SurfaceApi.GetPhysicalDeviceSurfaceCapabilities(device.PhysicalDevice, instance.Surface, out var capabilities);

        var surfaceFormat = ChooseSurfaceFormat();
        var presentMode = ChoosePresentMode();
        var extent = ChooseExtent(capabilities);

        var imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
        {
            imageCount = capabilities.MaxImageCount;
        }

        // TransferSrc lets a presented image be copied out (see FrameCapture). Requesting a usage
        // the surface does not advertise is invalid, so it is opt-in on capability.
        SupportsCapture = (capabilities.SupportedUsageFlags & ImageUsageFlags.TransferSrcBit) != 0;

        var imageUsage = ImageUsageFlags.ColorAttachmentBit;
        if (SupportsCapture)
        {
            imageUsage |= ImageUsageFlags.TransferSrcBit;
        }

        SwapchainCreateInfoKHR createInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = instance.Surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = imageUsage,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default
        };

        var queueFamilyIndices = stackalloc uint[] { device.GraphicsFamily, device.PresentFamily };
        if (device.GraphicsFamily != device.PresentFamily)
        {
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = queueFamilyIndices;
        }
        else
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        if (swapchainApi.CreateSwapchain(device.Handle, in createInfo, null, out var swapchain) != Result.Success)
        {
            throw new InvalidOperationException("Failed to create the swapchain.");
        }
        Handle = swapchain;

        uint actualImageCount = 0;
        swapchainApi.GetSwapchainImages(device.Handle, Handle, &actualImageCount, null);
        Images = new Image[actualImageCount];
        fixed (Image* pImages = Images)
        {
            swapchainApi.GetSwapchainImages(device.Handle, Handle, &actualImageCount, pImages);
        }

        ImageFormat = surfaceFormat.Format;
        Extent = extent;

        CreateImageViews();
    }

    private void CreateImageViews()
    {
        ImageViews = new ImageView[Images.Length];

        for (int i = 0; i < Images.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = Images[i],
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

            if (vk.CreateImageView(device.Handle, in createInfo, null, out ImageViews[i]) != Result.Success)
            {
                throw new InvalidOperationException("Failed to create a swapchain image view.");
            }
        }
    }

    private void DestroyImageViews()
    {
        foreach (var imageView in ImageViews)
        {
            vk.DestroyImageView(device.Handle, imageView, null);
        }

        ImageViews = [];
    }

    private SurfaceFormatKHR ChooseSurfaceFormat()
    {
        uint formatCount = 0;
        instance.SurfaceApi.GetPhysicalDeviceSurfaceFormats(device.PhysicalDevice, instance.Surface, &formatCount, null);

        var formats = new SurfaceFormatKHR[formatCount];
        fixed (SurfaceFormatKHR* pFormats = formats)
        {
            instance.SurfaceApi.GetPhysicalDeviceSurfaceFormats(device.PhysicalDevice, instance.Surface, &formatCount, pFormats);
        }

        foreach (var format in formats)
        {
            if (format.Format == Format.B8G8R8A8Srgb && format.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                return format;
            }
        }

        return formats[0];
    }

    private PresentModeKHR ChoosePresentMode()
    {
        uint presentModeCount = 0;
        instance.SurfaceApi.GetPhysicalDeviceSurfacePresentModes(device.PhysicalDevice, instance.Surface, &presentModeCount, null);

        var presentModes = new PresentModeKHR[presentModeCount];
        fixed (PresentModeKHR* pPresentModes = presentModes)
        {
            instance.SurfaceApi.GetPhysicalDeviceSurfacePresentModes(device.PhysicalDevice, instance.Surface, &presentModeCount, pPresentModes);
        }

        return presentModes.Contains(PresentModeKHR.MailboxKhr)
            ? PresentModeKHR.MailboxKhr
            : PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }

        var (width, height) = window.FramebufferSize;

        return new Extent2D
        {
            Width = Math.Clamp((uint)width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
            Height = Math.Clamp((uint)height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height)
        };
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        DestroyImageViews();
        swapchainApi.DestroySwapchain(device.Handle, Handle, null);
        disposed = true;
    }
}
