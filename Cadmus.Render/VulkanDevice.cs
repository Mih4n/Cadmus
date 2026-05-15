using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Cadmus.Render;

public unsafe class VulkanDevice : IDisposable
{
    private readonly Vk _vk;
    private readonly Instance _instance;
    private readonly SurfaceKHR _surface;
    private readonly KhrSurface _khrSurface;

    public PhysicalDevice PhysicalDevice { get; private set; }
    public Device Device { get; private set; }
    public Queue GraphicsQueue { get; private set; }
    public Queue PresentQueue { get; private set; }
    public uint GraphicsFamily { get; private set; }
    public uint PresentFamily { get; private set; }
    public CommandPool CommandPool { get; private set; }

    public VulkanDevice(Vk vk, Instance instance, SurfaceKHR surface, KhrSurface khrSurface)
    {
        _vk = vk;
        _instance = instance;
        _surface = surface;
        _khrSurface = khrSurface;

        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateCommandPool();
    }

    private void PickPhysicalDevice()
    {
        uint deviceCount = 0;
        _vk.EnumeratePhysicalDevices(_instance, &deviceCount, null);

        if (deviceCount == 0)
        {
            throw new Exception("Failed to find GPUs with Vulkan support!");
        }

        var devices = stackalloc PhysicalDevice[(int)deviceCount];
        _vk.EnumeratePhysicalDevices(_instance, &deviceCount, devices);

        for (int i = 0; i < deviceCount; i++)
        {
            if (IsDeviceSuitable(devices[i]))
            {
                PhysicalDevice = devices[i];
                break;
            }
        }

        if (PhysicalDevice.Handle == 0)
        {
            throw new Exception("Failed to find a suitable GPU!");
        }
    }

    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indices = FindQueueFamilies(device);
        bool extensionsSupported = CheckDeviceExtensionSupport(device);
        bool swapChainAdequate = false;

        if (extensionsSupported)
        {
            _khrSurface.GetPhysicalDeviceSurfaceCapabilities(device, _surface, out var surfaceCapabilities);
            uint formatCount = 0;
            _khrSurface.GetPhysicalDeviceSurfaceFormats(device, _surface, &formatCount, null);
            uint presentModeCount = 0;
            _khrSurface.GetPhysicalDeviceSurfacePresentModes(device, _surface, &presentModeCount, null);
            swapChainAdequate = formatCount > 0 && presentModeCount > 0;
        }

        return indices.IsComplete && extensionsSupported && swapChainAdequate;
    }

    private bool CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        uint extensionCount = 0;
        _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);
        var availableExtensions = stackalloc ExtensionProperties[(int)extensionCount];
        _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, availableExtensions);

        var requiredExtensions = new HashSet<string> { "VK_KHR_swapchain" };

        for (int i = 0; i < extensionCount; i++)
        {
            string name = Marshal.PtrToStringAnsi((nint)availableExtensions[i].ExtensionName) ?? "";
            requiredExtensions.Remove(name);
        }

        return requiredExtensions.Count == 0;
    }

    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        uint queueFamilyCount = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);
        var queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

        var indices = new QueueFamilyIndices();

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if ((queueFamilies[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
            {
                indices.GraphicsFamily = i;
            }

            _khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out var presentSupport);
            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete)
                break;
        }

        return indices;
    }

    private void CreateLogicalDevice()
    {
        var indices = FindQueueFamilies(PhysicalDevice);
        GraphicsFamily = indices.GraphicsFamily!.Value;
        PresentFamily = indices.PresentFamily!.Value;

        var uniqueQueueFamilies = new HashSet<uint> { GraphicsFamily, PresentFamily };
        float queuePriority = 1.0f;

        var queueCreateInfos = stackalloc DeviceQueueCreateInfo[uniqueQueueFamilies.Count];
        int qIndex = 0;
        foreach (var queueFamily in uniqueQueueFamilies)
        {
            queueCreateInfos[qIndex] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = queueFamily,
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
            qIndex++;
        }

        PhysicalDeviceFeatures deviceFeatures = new();

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Count,
            PQueueCreateInfos = queueCreateInfos,
            PEnabledFeatures = &deviceFeatures,
            EnabledExtensionCount = 1
        };

        byte* swapchainExtension = (byte*)Marshal.StringToHGlobalAnsi("VK_KHR_swapchain");
        createInfo.PpEnabledExtensionNames = &swapchainExtension;

        if (_vk.CreateDevice(PhysicalDevice, in createInfo, null, out Device device) != Result.Success)
        {
            throw new Exception("Failed to create logical device!");
        }

        Device = device;
        Marshal.FreeHGlobal((nint)swapchainExtension);

        _vk.GetDeviceQueue(Device, GraphicsFamily, 0, out Queue graphicsQueue);
        GraphicsQueue = graphicsQueue;
        _vk.GetDeviceQueue(Device, PresentFamily, 0, out Queue presentQueue);
        PresentQueue = presentQueue;
    }

    private void CreateCommandPool()
    {
        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = GraphicsFamily,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };

        if (_vk.CreateCommandPool(Device, in poolInfo, null, out CommandPool pool) != Result.Success)
        {
            throw new Exception("Failed to create command pool!");
        }

        CommandPool = pool;
    }

    public Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            _vk.GetPhysicalDeviceFormatProperties(PhysicalDevice, format, out var props);
            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
            {
                return format;
            }
        }
        throw new Exception("Failed to find supported format!");
    }

    public Format FindDepthFormat()
    {
        return FindSupportedFormat(
            new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint },
            ImageTiling.Optimal,
            FormatFeatureFlags.DepthStencilAttachmentBit);
    }

    public void Dispose()
    {
        _vk.DestroyCommandPool(Device, CommandPool, null);
        _vk.DestroyDevice(Device, null);
    }

    private struct QueueFamilyIndices
    {
        public uint? GraphicsFamily;
        public uint? PresentFamily;
        public bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
}
