namespace Cadmus.Domain.Contracts;

public interface IEntity : IComposeComponent
{
    Guid Id { get; }
}
