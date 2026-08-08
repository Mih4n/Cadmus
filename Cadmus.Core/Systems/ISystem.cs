using Cadmus.Core.Game;

namespace Cadmus.Core.Systems;

/// <summary>
/// A per-frame processor. Systems are resolved from DI and receive their dependencies through the
/// constructor — the current scene comes from an injected <c>ISceneManager</c>, not from a context
/// argument.
/// </summary>
public interface ISystem
{
    /// <summary>Lower values update first. Renderers conventionally sit at the end.</summary>
    int Order => 0;

    ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default);
}
