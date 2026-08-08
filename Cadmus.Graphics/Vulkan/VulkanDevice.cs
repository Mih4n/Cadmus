using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

/// <summary>
/// Physical/logical device selection, queues and the shared command pool.
/// </summary>
public sealed unsafe class VulkanDevice : IDisposable
{
    private readonly VulkanInstance instance;

    public Vk Api { get; }
    public PhysicalDevice PhysicalDevice { get; }
    public Device Handle { get; }
    public Queue GraphicsQueue { get; }
    public Queue PresentQueue { get; }
    public uint GraphicsFamily { get; }
    public uint PresentFamily { get; }
    public CommandPool CommandPool { get; }
    public PhysicalDeviceProperties Properties { get; }

    /// <summary>Human-readable adapter name, e.g. for a debug HUD.</summary>
    public string Name { get; }
    public bool SupportsAnisotropy { get; }

    /// <summary>Swapchain entry points, loaded once the logical device exists.</summary>
    public KhrSwapchain SwapchainApi { get; }

    public VulkanDevice(VulkanInstance instance)
    {
        this.instance = instance;
        Api = instance.Api;

        PhysicalDevice = PickPhysicalDevice();
        Api.GetPhysicalDeviceProperties(PhysicalDevice, out var properties);
        Properties = properties;

        Api.GetPhysicalDeviceFeatures(PhysicalDevice, out var supportedFeatures);
        SupportsAnisotropy = supportedFeatures.SamplerAnisotropy;

        var indices = FindQueueFamilies(PhysicalDevice);
        GraphicsFamily = indices.GraphicsFamily!.Value;
        PresentFamily = indices.PresentFamily!.Value;

        Handle = CreateLogicalDevice();

        Api.GetDeviceQueue(Handle, GraphicsFamily, 0, out var graphicsQueue);
        GraphicsQueue = graphicsQueue;
        Api.GetDeviceQueue(Handle, PresentFamily, 0, out var presentQueue);
        PresentQueue = presentQueue;

        if (!Api.TryGetDeviceExtension(instance.Handle, Handle, out KhrSwapchain swapchainApi))
        {
            throw new InvalidOperationException("Failed to load the VK_KHR_swapchain extension.");
        }
        SwapchainApi = swapchainApi;

        CommandPool = CreateCommandPool();

        // Read from the local copy: a fixed-size buffer cannot be addressed through a property.
        Name = Marshal.PtrToStringAnsi((nint)properties.DeviceName) ?? "unknown";
        Console.WriteLine($"[Cadmus] Vulkan device: {Name}");
    }

    private PhysicalDevice PickPhysicalDevice()
    {
        uint deviceCount = 0;
        Api.EnumeratePhysicalDevices(instance.Handle, &deviceCount, null);

        if (deviceCount == 0)
        {
            throw new InvalidOperationException("No GPU with Vulkan support was found.");
        }

        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* pDevices = devices)
        {
            Api.EnumeratePhysicalDevices(instance.Handle, &deviceCount, pDevices);
        }

        // Prefer a discrete GPU, but accept any suitable one.
        PhysicalDevice fallback = default;
        foreach (var device in devices)
        {
            if (!IsDeviceSuitable(device))
            {
                continue;
            }

            Api.GetPhysicalDeviceProperties(device, out var properties);
            if (properties.DeviceType == PhysicalDeviceType.DiscreteGpu)
            {
                return device;
            }

            if (fallback.Handle == 0)
            {
                fallback = device;
            }
        }

        if (fallback.Handle == 0)
        {
            throw new InvalidOperationException("No suitable GPU was found (needs graphics + present + swapchain).");
        }

        return fallback;
    }

    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indices = FindQueueFamilies(device);
        if (!indices.IsComplete || !SupportsSwapchainExtension(device))
        {
            return false;
        }

        uint formatCount = 0;
        instance.SurfaceApi.GetPhysicalDeviceSurfaceFormats(device, instance.Surface, &formatCount, null);
        uint presentModeCount = 0;
        instance.SurfaceApi.GetPhysicalDeviceSurfacePresentModes(device, instance.Surface, &presentModeCount, null);

        return formatCount > 0 && presentModeCount > 0;
    }

    private bool SupportsSwapchainExtension(PhysicalDevice device)
    {
        uint extensionCount = 0;
        Api.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);

        var extensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* pExtensions = extensions)
        {
            Api.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, pExtensions);

            for (int i = 0; i < extensionCount; i++)
            {
                if (Marshal.PtrToStringAnsi((nint)pExtensions[i].ExtensionName) == KhrSwapchain.ExtensionName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        uint queueFamilyCount = 0;
        Api.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        var indices = new QueueFamilyIndices();

        fixed (QueueFamilyProperties* pQueueFamilies = queueFamilies)
        {
            Api.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, pQueueFamilies);

            for (uint i = 0; i < queueFamilyCount; i++)
            {
                if ((pQueueFamilies[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
                {
                    indices.GraphicsFamily = i;
                }

                instance.SurfaceApi.GetPhysicalDeviceSurfaceSupport(device, i, instance.Surface, out var presentSupport);
                if (presentSupport)
                {
                    indices.PresentFamily = i;
                }

                if (indices.IsComplete)
                {
                    break;
                }
            }
        }

        return indices;
    }

    private Device CreateLogicalDevice()
    {
        var uniqueFamilies = new HashSet<uint> { GraphicsFamily, PresentFamily };
        float queuePriority = 1f;

        var queueCreateInfos = stackalloc DeviceQueueCreateInfo[uniqueFamilies.Count];
        int index = 0;
        foreach (var family in uniqueFamilies)
        {
            queueCreateInfos[index++] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = family,
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        // Only request features the device actually reports, or device creation fails.
        PhysicalDeviceFeatures features = new()
        {
            SamplerAnisotropy = SupportsAnisotropy
        };

        var extensionNames = (byte**)SilkMarshal.StringArrayToPtr(new[] { KhrSwapchain.ExtensionName });

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueFamilies.Count,
            PQueueCreateInfos = queueCreateInfos,
            PEnabledFeatures = &features,
            EnabledExtensionCount = 1,
            PpEnabledExtensionNames = extensionNames
        };

        try
        {
            var result = Api.CreateDevice(PhysicalDevice, in createInfo, null, out var device);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create the logical device: {result}.");
            }

            return device;
        }
        finally
        {
            SilkMarshal.Free((nint)extensionNames);
        }
    }

    private CommandPool CreateCommandPool()
    {
        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = GraphicsFamily,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };

        if (Api.CreateCommandPool(Handle, in poolInfo, null, out var pool) != Result.Success)
        {
            throw new InvalidOperationException("Failed to create the command pool.");
        }

        return pool;
    }

    /// <summary>Records, submits and waits on a one-shot command buffer.</summary>
    public void SubmitImmediate(Action<CommandBuffer> record)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = CommandPool,
            CommandBufferCount = 1
        };

        Api.AllocateCommandBuffers(Handle, in allocInfo, out var commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Api.BeginCommandBuffer(commandBuffer, in beginInfo);
        record(commandBuffer);
        Api.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        Api.QueueSubmit(GraphicsQueue, 1, in submitInfo, default);
        Api.QueueWaitIdle(GraphicsQueue);
        Api.FreeCommandBuffers(Handle, CommandPool, 1, in commandBuffer);
    }

    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        Api.GetPhysicalDeviceMemoryProperties(PhysicalDevice, out var memoryProperties);

        for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << (int)i)) != 0 &&
                (memoryProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }

        throw new InvalidOperationException("No suitable memory type was found.");
    }

    public Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            Api.GetPhysicalDeviceFormatProperties(PhysicalDevice, format, out var properties);

            var supported = tiling == ImageTiling.Linear
                ? properties.LinearTilingFeatures
                : properties.OptimalTilingFeatures;

            if ((supported & features) == features)
            {
                return format;
            }
        }

        throw new InvalidOperationException("No supported format was found.");
    }

    public Format FindDepthFormat() => FindSupportedFormat(
        [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint],
        ImageTiling.Optimal,
        FormatFeatureFlags.DepthStencilAttachmentBit);

    public void WaitIdle() => Api.DeviceWaitIdle(Handle);

    public void Dispose()
    {
        Api.DestroyCommandPool(Handle, CommandPool, null);
        SwapchainApi.Dispose();
        Api.DestroyDevice(Handle, null);
    }

    private struct QueueFamilyIndices
    {
        public uint? GraphicsFamily;
        public uint? PresentFamily;
        public readonly bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
}
