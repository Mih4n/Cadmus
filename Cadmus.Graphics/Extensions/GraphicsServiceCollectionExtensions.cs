using Cadmus.Core.Input;
using Cadmus.Core.Windowing;
using Cadmus.Engine.Extensions;
using Cadmus.Graphics.Input;
using Cadmus.Graphics.Resources;
using Cadmus.Graphics.Vulkan;
using Cadmus.Graphics.Windowing;
using Cadmus.Graphics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.Graphics.Extensions;

public static class GraphicsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the window and the whole Vulkan object graph as singletons. Each object declares
    /// what it needs in its constructor, so the container — not a hand-written init routine —
    /// decides creation order, and disposal happens in reverse.
    /// </summary>
    public static IServiceCollection AddCadmusGraphics(this IServiceCollection services)
    {
        services.TryAddSingleton<GameWindowOptions>();
        services.TryAddSingleton<VulkanOptions>();

        services.TryAddSingleton<SilkGameWindow>();
        services.TryAddSingleton<IGameWindow>(sp => sp.GetRequiredService<SilkGameWindow>());

        // Registered as a system as well, so its per-frame state refresh is driven by the host.
        services.AddSystem<SilkInputService>();
        services.TryAddSingleton<IInputService>(sp => sp.GetRequiredService<SilkInputService>());

        services.TryAddSingleton<VulkanInstance>();
        services.TryAddSingleton<VulkanDevice>();
        services.TryAddSingleton<VulkanSwapchain>();
        services.TryAddSingleton<VulkanRenderPass>();
        services.TryAddSingleton<VulkanFramebuffers>();
        services.TryAddSingleton<VulkanFrameSync>();
        services.TryAddSingleton<VulkanCommandBuffers>();
        services.TryAddSingleton<VulkanPipeline>();
        services.TryAddSingleton<UniformRing>();

        services.TryAddSingleton<IFrameCapture, FrameCapture>();

        services.TryAddSingleton<GpuResourceCache>();
        services.TryAddSingleton<IGpuResourceCache>(sp => sp.GetRequiredService<GpuResourceCache>());

        return services;
    }

    public static IServiceCollection ConfigureWindow(this IServiceCollection services, Action<GameWindowOptions> configure)
    {
        var options = services.GetOrAddSingletonInstance(() => new GameWindowOptions());
        configure(options);

        return services;
    }

    public static IServiceCollection ConfigureVulkan(this IServiceCollection services, Action<VulkanOptions> configure)
    {
        var options = services.GetOrAddSingletonInstance(() => new VulkanOptions());
        configure(options);

        return services;
    }

    private static T GetOrAddSingletonInstance<T>(this IServiceCollection services, Func<T> create) where T : class
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));

        if (descriptor?.ImplementationInstance is T existing)
        {
            return existing;
        }

        var instance = create();

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton(instance);

        return instance;
    }
}
