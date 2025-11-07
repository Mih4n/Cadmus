using Cadmus.Domain.Contracts.Components;

namespace Cadmus.Domain.Contracts.Entities;

public interface IEntity : IComposeComponent
{
    Guid Id { get; }
}
