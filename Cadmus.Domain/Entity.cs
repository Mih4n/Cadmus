using Cadmus.Domain.Contracts;

namespace Cadmus.Domain;

public class Entity : ComposeComponent, IEntity
{
    public Guid Id { get; } = Guid.CreateVersion7(); 
}
