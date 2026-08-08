using Cadmus.Core.Components;

namespace Cadmus.Core.Entities;

public interface IEntity : IComposeComponent
{
    Guid Id { get; }
    string Name { get; }
    bool IsEnabled { get; set; }
}
