using Cadmus.Core.Scenes;
using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.Engine.Scenes;

/// <summary>
/// Resolves scenes from the container by their registered name. Each scene lives in its own service
/// scope so per-scene state is released on unload.
/// </summary>
public sealed class SceneManager(IServiceProvider services, ISceneRegistry registry) : ISceneManager, IDisposable
{
    private IServiceScope? currentScope;

    public IScene? Current { get; private set; }

    public IReadOnlyCollection<string> RegisteredScenes => registry.Scenes.Keys.ToArray();

    public event Action<IScene?>? SceneChanged;

    public async Task LoadAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!registry.Scenes.TryGetValue(name, out var sceneType))
        {
            throw new KeyNotFoundException(
                $"Scene '{name}' is not registered. Known scenes: {string.Join(", ", registry.Scenes.Keys)}");
        }

        await UnloadCurrentAsync(cancellationToken);

        var scope = services.CreateScope();
        try
        {
            var scene = (IScene)ActivatorUtilities.CreateInstance(scope.ServiceProvider, sceneType);
            await scene.LoadAsync(cancellationToken);

            currentScope = scope;
            Current = scene;
        }
        catch
        {
            scope.Dispose();
            throw;
        }

        SceneChanged?.Invoke(Current);
    }

    public async Task UnloadCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (Current is null)
        {
            return;
        }

        await Current.UnloadAsync(cancellationToken);
        Current = null;

        currentScope?.Dispose();
        currentScope = null;

        SceneChanged?.Invoke(null);
    }

    public void Dispose()
    {
        currentScope?.Dispose();
        currentScope = null;
        Current = null;
    }
}
