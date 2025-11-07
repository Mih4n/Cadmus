using Cadmus.Domain.Components;
using Cadmus.Domain.Contracts;
using Cadmus.Domain.Contracts.Entities;

namespace Cadmus.Domain.Entities;

public class Entity : ComposeComponent, IEntity
{
    public Guid Id { get; } = Guid.CreateVersion7(); 
}
