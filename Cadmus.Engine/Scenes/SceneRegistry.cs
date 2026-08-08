using Cadmus.Core.Scenes;

namespace Cadmus.Engine.Scenes;

/// <inheritdoc cref="ISceneRegistry"/>
public sealed class SceneRegistry : ISceneRegistry
{
    private readonly Dictionary<string, Type> scenes = [];

    public IReadOnlyDictionary<string, Type> Scenes => scenes;

    public void Register(string name, Type sceneType)
    {
        if (!typeof(IScene).IsAssignableFrom(sceneType))
        {
            throw new ArgumentException($"{sceneType.Name} does not implement {nameof(IScene)}.", nameof(sceneType));
        }

        scenes[name] = sceneType;
    }
}
