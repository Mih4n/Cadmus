using Cadmus.Core.Entities;

namespace Cadmus.Core.Storage;

public interface IEntityStorage
{
    public void Add(IEntity entity);
    public void Remove(IEntity entity);
    public void AddComponent<T>(IEntity entity, T component) where T : struct;
    public void RemoveComponent<T>(IEntity entity, T component) where T : struct;

    public void Query<T>(Action<Span<T>> action) where T : struct;
    public void Query<T1, T2>(Action<Span<T1>, Span<T2>> action) where T1 : struct where T2 : struct;
}
