using Cadmus.Core.Diagnostics;
using Cadmus.Engine.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cadmus.Rendering.Extensions;

public static class RenderingServiceCollectionExtensions
{
    /// <summary>Registers the built-in rendering systems.</summary>
    public static IServiceCollection AddCadmusRendering(this IServiceCollection services)
    {
        // Transient: the collector reuses one internal list, so each system needs its own.
        services.TryAddTransient<RenderItemCollector>();

        services.AddSystem<ResourceUploadSystem>();
        services.AddSystem<DebugOverlay>();
        services.AddSystem<VulkanRenderSystem>();

        services.TryAddSingleton<IDebugOverlay>(sp => sp.GetRequiredService<DebugOverlay>());

        return services;
    }
}
