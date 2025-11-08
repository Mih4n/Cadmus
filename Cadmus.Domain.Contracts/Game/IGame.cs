using Cadmus.Domain.Contracts.Components;
using Cadmus.Domain.Contracts.Systems;

namespace Cadmus.Domain.Contracts;

public interface IGame : IComposeComponent
{
    IScene? CurrentScene { get; }
    IReadOnlyDictionary<Type, ISystem> Systems { get; }
    IReadOnlyDictionary<string, IScene> Scenes { get; }

    Task Update();
    void SetSystem<T>(T system) where T : ISystem;
    void RemoveSystem<T>() where T : ISystem;
    void RegisterScene(string name, IScene scene);
    bool UnregisterScene(string name);
    Task LoadSceneAsync(string sceneName);
    Task InitializeAsync();
}

