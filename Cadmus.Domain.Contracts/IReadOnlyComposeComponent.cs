using System.Diagnostics.CodeAnalysis;

namespace Cadmus.Domain.Contracts;

public interface IReadOnlyComposeComponent
{
    T? GetComponent<T>() where T : IComponent;
    bool TryGetComponent<T>([MaybeNullWhen(false)] out T component) where T : IComponent;
    bool HasComponent<T>() where T : IComponent; 
}
