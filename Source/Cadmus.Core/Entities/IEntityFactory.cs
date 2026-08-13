namespace Cadmus.Core.Entities;

public interface IEntityFactory
{
    T Create<T>() where T : class, IEntity;
}
