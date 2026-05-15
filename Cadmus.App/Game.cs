using Cadmus.Domain.Components;
using Cadmus.Domain.Contracts;
using Cadmus.Domain.Contracts.Components;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;
using Cadmus.Domain.Game;
using Cadmus.Render;
using Cadmus.Systems.Rendering;
using Silk.NET.Windowing;

namespace Cadmus.App;

public abstract class Game : ComposeComponent, IGame 
{
    private string? currentScene;
    private IGameContext context;
    private Dictionary<Type, ISystem> systems = [];
    private Dictionary<string, IScene> scenes = [];
    private VulkanRenderingContext renderContext = null!;

    public bool IsRunning { get; private set; }
    public IScene? CurrentScene => currentScene != null ? Scenes.GetValueOrDefault(currentScene) : null;
    public IReadOnlyDictionary<Type, ISystem> Systems => systems;
    public IReadOnlyDictionary<string, IScene> Scenes => scenes;

    public Game()
    {
        context = new GameContext(this);
        renderContext = new VulkanRenderingContext();
        AddComponent(renderContext);
        
        SetSystem(new VulkanRenderer(context));
        SetSystem(new TextureLoadSystem(context));
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
        if (!IsRunning)
        {
            return;
        }

        var window = renderContext.Window;
        window.DoEvents();

        if (window.IsClosing)
        {
            IsRunning = false;
            return;
        }

        // Update all non-renderer systems in parallel
        var updateTasks = systems
            .Values
            .Where(s => s is not IRenderer)
            .Select(s => s.Update(context))
            .ToArray();

        await Task.WhenAll(updateTasks);

        // Render sequentially
        foreach (var renderer in systems.Values.OfType<IRenderer>())
        {
            renderer.Render();
        }
    }

    public Task LoadSceneAsync(string sceneName)
    {
        currentScene = sceneName;
        return Task.CompletedTask;
    }

    public void Start()
    {
        IsRunning = true;
    }
}
