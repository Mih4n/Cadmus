namespace Cadmus.Domain.Contracts;

public interface IEntity : IComposeComponent
{
    public Guid Id { get; }
}
