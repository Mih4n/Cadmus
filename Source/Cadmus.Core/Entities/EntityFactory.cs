using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.Core.Entities;

public class EntityFactory(IServiceProvider provider) : IEntityFactory
{
    public T Create<T>() where T : class, IEntity
    {
        if (typeof(T) == typeof(Entity))
            return (T)(IEntity)new Entity();
        return provider.GetRequiredService<T>();
    }
}
