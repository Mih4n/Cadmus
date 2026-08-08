namespace Cadmus.Core.Scenes;

/// <summary>
/// Owns the active scene. Systems inject this instead of receiving a context object, so a system
/// always sees the scene that is current right now.
/// </summary>
public interface ISceneManager
{
    IScene? Current { get; }
    IReadOnlyCollection<string> RegisteredScenes { get; }

    /// <summary>Unloads the current scene and loads the scene registered under <paramref name="name"/>.</summary>
    Task LoadAsync(string name, CancellationToken cancellationToken = default);

    Task UnloadCurrentAsync(CancellationToken cancellationToken = default);

    event Action<IScene?>? SceneChanged;
}

/// <summary>
/// Name → scene type map filled at container-configuration time by <c>AddScene&lt;T&gt;</c>.
/// </summary>
public interface ISceneRegistry
{
    IReadOnlyDictionary<string, Type> Scenes { get; }

    void Register(string name, Type sceneType);
}
