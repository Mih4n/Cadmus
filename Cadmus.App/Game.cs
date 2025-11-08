using Cadmus.Domain.Components;
using Cadmus.Domain.Contracts;
using Cadmus.Domain.Contracts.Components;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;
using Cadmus.Domain.Game;

namespace Cadmus.App;

public abstract class Game : ComposeComponent, IGame 
{
    private string? currentScene;
    private IGameContext context;
    private Dictionary<Type, ISystem> systems = [];
    private Dictionary<string, IScene> scenes = [];

    public IScene? CurrentScene => currentScene != null ? Scenes.GetValueOrDefault(currentScene) : null;
    public IReadOnlyDictionary<Type, ISystem> Systems => systems;
    public IReadOnlyDictionary<string, IScene> Scenes => scenes;

    public Game()
    {
        context = new GameContext(this);
    }

    public abstract Task InitializeAsync();

    public void RegisterScene(string name, IScene scene)
    {
        scenes.Add(name, scene);
    }

    public bool UnregisterScene(string name)
    {
        return scenes.Remove(name);
    }
    
    public void SetSystem<T>(T system) where T : ISystem
    {
        systems[typeof(T)] = system;
    }

    public void RemoveSystem<T>() where T : ISystem
    {
        systems.Remove(typeof(T));
    }

    public async Task Update()
    {
        var toUpdate = systems
            .Values
            .Select(s => s.Update(context))
            .ToArray();

        await Task.WhenAll(toUpdate);   
    }

    public Task LoadSceneAsync(string sceneName)
    {
        currentScene = sceneName;
        return Task.CompletedTask;
    }
}
