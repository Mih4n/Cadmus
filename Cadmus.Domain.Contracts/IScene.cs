using System.Collections.ObjectModel;

namespace Cadmus.Domain.Contracts;

public interface IScene : IEntity
{
    ReadOnlyDictionary<Guid, IEntity> Entities { get; }
}