using Cadmus.Core.Diagnostics;
using Cadmus.Core.Entities;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Engine.Diagnostics;
using Cadmus.Engine.Entities;
using Cadmus.Engine.Scenes;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.Engine.Extensions;

public static class EngineServiceCollectionExtensions
{
    /// <summary>Registers the scene/entity infrastructure. Called for you by <c>AddCadmus</c>.</summary>
    public static IServiceCollection AddCadmusEngine(this IServiceCollection services)
    {
        services.TryAddSingleton<ISceneRegistry, SceneRegistry>();
        services.TryAddSingleton<ISceneManager, SceneManager>();
        services.TryAddSingleton<IEntityFactory, EntityFactory>();

        services.AddSystem<FrameStatistics>();
        services.TryAddSingleton<IFrameStatistics>(sp => sp.GetRequiredService<FrameStatistics>());

        return services;
    }

    /// <summary>
    /// Registers a scene under <paramref name="name"/>. The scene itself is created per load from
    /// the container, so its constructor dependencies are injected.
    /// </summary>
    public static IServiceCollection AddScene<TScene>(this IServiceCollection services, string name)
        where TScene : class, IScene
    {
        services.AddCadmusEngine();
        services.TryAddTransient<TScene>();

        // The registry is a singleton instance so it can be filled before the provider is built.
        var registry = services.GetSceneRegistry();
        registry.Register(name, typeof(TScene));

        return services;
    }

    /// <summary>
    /// Registers a system as a singleton and adds it to the <c>IEnumerable&lt;ISystem&gt;</c> the
    /// host iterates, in <see cref="ISystem.Order"/> order.
    /// </summary>
    public static IServiceCollection AddSystem<TSystem>(this IServiceCollection services)
        where TSystem : class, ISystem
    {
        services.TryAddSingleton<TSystem>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISystem, TSystem>(sp => sp.GetRequiredService<TSystem>()));

        return services;
    }

    /// <summary>
    /// Registers an entity type so it can be resolved with its dependencies via
    /// <see cref="IEntityFactory"/>.
    /// </summary>
    public static IServiceCollection AddEntity<TEntity>(this IServiceCollection services)
        where TEntity : class, IEntity
    {
        services.AddCadmusEngine();
        services.TryAddTransient<TEntity>();

        return services;
    }

    private static ISceneRegistry GetSceneRegistry(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISceneRegistry));

        if (descriptor?.ImplementationInstance is ISceneRegistry existing)
        {
            return existing;
        }

        var registry = new SceneRegistry();

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<ISceneRegistry>(registry);

        return registry;
    }
}
