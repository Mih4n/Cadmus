namespace Cadmus.Core.Entities;

public class Entity : IEntity
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
}
