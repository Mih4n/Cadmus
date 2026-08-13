using Cadmus.Core.Components;
using Cadmus.Core.Entities;
using Cadmus.Core.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.Engine;

public static class Extender
{
    public static IServiceCollection AddCadmusEngine(this IServiceCollection services)
    {
        services.AddSingleton<IComponentDescriptor, ComponentDescriptor>();
        services.AddSingleton<IEntityFactory, EntityFactory>();
        services.AddSingleton<IEntityStorage, EntityStorage>();
        services.AddSingleton<World>();
        services.AddSingleton<SystemScheduler>();

        return services;
    }

    public static IServiceCollection AddSystem<TSystem>(this IServiceCollection services) where TSystem : class, ISystem
    {
        services.AddSingleton<ISystem, TSystem>();
        return services;
    }
}
