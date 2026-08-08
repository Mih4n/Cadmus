using System.Runtime.InteropServices;
using Cadmus.Graphics.Vulkan;
using Cadmus.Graphics.Windowing;
using Cadmus.Graphics;
using Silk.NET.Core.Native;
using Silk.NET.Core;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

/// <summary>
/// Owns the Vulkan API entry point, the instance and the window surface. Everything else in the
/// backend takes this as a constructor dependency instead of creating its own.
/// </summary>
public sealed unsafe class VulkanInstance : IDisposable
{
    private const string ValidationLayerName = "VK_LAYER_KHRONOS_validation";

    private readonly ExtDebugUtils? debugUtils;
    private readonly DebugUtilsMessengerEXT debugMessenger;
    private readonly PfnDebugUtilsMessengerCallbackEXT debugCallback;

    public Vk Api { get; }
    public Instance Handle { get; }
    public SurfaceKHR Surface { get; }
    public KhrSurface SurfaceApi { get; }
    public bool ValidationEnabled { get; }

    public VulkanInstance(SilkGameWindow window, VulkanOptions options)
    {
        Api = Vk.GetApi();
        ValidationEnabled = options.EnableValidation && IsValidationLayerAvailable();

        if (options.EnableValidation && !ValidationEnabled)
        {
            Console.WriteLine($"[Cadmus] {ValidationLayerName} not installed — continuing without validation.");
        }

        Handle = CreateInstance(window, options);

        if (ValidationEnabled && Api.TryGetInstanceExtension(Handle, out ExtDebugUtils utils))
        {
            debugUtils = utils;
            debugCallback = new PfnDebugUtilsMessengerCallbackEXT(OnDebugMessage);

            var messengerInfo = CreateMessengerInfo(debugCallback);
            if (debugUtils.CreateDebugUtilsMessenger(Handle, in messengerInfo, null, out var messenger) == Result.Success)
            {
                debugMessenger = messenger;
            }
        }

        if (!Api.TryGetInstanceExtension(Handle, out KhrSurface khrSurface))
        {
            throw new InvalidOperationException("Failed to load the VK_KHR_surface extension.");
        }
        SurfaceApi = khrSurface;

        var surfaceHandle = window.Native.VkSurface!.Create<AllocationCallbacks>(new VkHandle(Handle.Handle), null);
        Surface = new SurfaceKHR(surfaceHandle.Handle);
    }

    private Instance CreateInstance(SilkGameWindow window, VulkanOptions options)
    {
        var applicationName = (byte*)Marshal.StringToHGlobalAnsi(options.ApplicationName);
        var engineName = (byte*)Marshal.StringToHGlobalAnsi("Cadmus");

        ApplicationInfo applicationInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = applicationName,
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = engineName,
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        var requiredExtensions = window.Native.VkSurface!.GetRequiredExtensions(out var extensionCount);
        var extensions = new List<string>();
        for (uint i = 0; i < extensionCount; i++)
        {
            extensions.Add(Marshal.PtrToStringAnsi((nint)requiredExtensions[i])!);
        }

        if (ValidationEnabled)
        {
            extensions.Add(ExtDebugUtils.ExtensionName);
        }

        var extensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
        var layerNames = ValidationEnabled
            ? (byte**)SilkMarshal.StringArrayToPtr(new[] { ValidationLayerName })
            : null;

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &applicationInfo,
            EnabledExtensionCount = (uint)extensions.Count,
            PpEnabledExtensionNames = extensionNames,
            EnabledLayerCount = ValidationEnabled ? 1u : 0u,
            PpEnabledLayerNames = layerNames
        };

        // Wired up before instance creation so layer errors during vkCreateInstance are reported too.
        var messengerInfo = CreateMessengerInfo(new PfnDebugUtilsMessengerCallbackEXT(OnDebugMessage));
        if (ValidationEnabled)
        {
            createInfo.PNext = &messengerInfo;
        }

        try
        {
            var result = Api.CreateInstance(in createInfo, null, out var instance);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create the Vulkan instance: {result}.");
            }

            return instance;
        }
        finally
        {
            SilkMarshal.Free((nint)extensionNames);
            if (layerNames is not null) SilkMarshal.Free((nint)layerNames);
            Marshal.FreeHGlobal((nint)applicationName);
            Marshal.FreeHGlobal((nint)engineName);
        }
    }

    private static DebugUtilsMessengerCreateInfoEXT CreateMessengerInfo(PfnDebugUtilsMessengerCallbackEXT callback) => new()
    {
        SType = StructureType.DebugUtilsMessengerCreateInfoExt,
        MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                        | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
        MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                    | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
                    | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
        PfnUserCallback = callback
    };

    private static uint OnDebugMessage(
        DebugUtilsMessageSeverityFlagsEXT severity,
        DebugUtilsMessageTypeFlagsEXT type,
        DebugUtilsMessengerCallbackDataEXT* callbackData,
        void* userData)
    {
        var message = Marshal.PtrToStringAnsi((nint)callbackData->PMessage);
        Console.WriteLine($"[Vulkan:{severity}] {message}");
        return Vk.False;
    }

    private bool IsValidationLayerAvailable()
    {
        uint layerCount = 0;
        Api.EnumerateInstanceLayerProperties(&layerCount, null);

        var layers = new LayerProperties[layerCount];
        fixed (LayerProperties* pLayers = layers)
        {
            Api.EnumerateInstanceLayerProperties(&layerCount, pLayers);

            for (int i = 0; i < layerCount; i++)
            {
                var name = Marshal.PtrToStringAnsi((nint)pLayers[i].LayerName);
                if (name == ValidationLayerName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Dispose()
    {
        SurfaceApi.DestroySurface(Handle, Surface, null);
        SurfaceApi.Dispose();

        if (debugUtils is not null && debugMessenger.Handle != 0)
        {
            debugUtils.DestroyDebugUtilsMessenger(Handle, debugMessenger, null);
            debugUtils.Dispose();
        }

        Api.DestroyInstance(Handle, null);
        Api.Dispose();
    }
}
