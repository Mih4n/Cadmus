namespace Cadmus.Core.Entities;

public interface IEntity
{
    public Guid Id { get; }
    public string Name { get; set; }
}
