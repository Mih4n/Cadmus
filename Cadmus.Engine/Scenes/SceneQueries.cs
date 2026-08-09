using Cadmus.Core.Components;
using Cadmus.Core.Entities;
using Cadmus.Core.Scenes;

namespace Cadmus.Engine.Scenes;

/// <summary>
/// How a system finds the entities it operates on: by the components they carry, never by holding a
/// reference to a particular entity. Disabled entities are skipped.
/// </summary>
/// <remarks>
/// The scene is nullable throughout so a system can write <c>scenes.Current.Query&lt;T&gt;()</c>
/// and simply do nothing while no scene is loaded.
/// </remarks>
public static class SceneQueries
{
    public static IEnumerable<(IEntity Entity, TComponent Component)> Query<TComponent>(this IScene? scene)
        where TComponent : IComponent
    {
        if (scene is null)
        {
            yield break;
        }

        foreach (var (_, entity) in scene.Entities)
        {
            if (entity.IsEnabled && entity.TryGetComponent<TComponent>(out var component) && component.IsActive)
            {
                yield return (entity, component);
            }
        }
    }

    public static IEnumerable<(IEntity Entity, TFirst First, TSecond Second)> Query<TFirst, TSecond>(this IScene? scene)
        where TFirst : IComponent
        where TSecond : IComponent
    {
        if (scene is null)
        {
            yield break;
        }

        foreach (var (_, entity) in scene.Entities)
        {
            if (entity.IsEnabled &&
                entity.TryGetComponent<TFirst>(out var first) && first.IsActive &&
                entity.TryGetComponent<TSecond>(out var second) && second.IsActive)
            {
                yield return (entity, first, second);
            }
        }
    }

    /// <summary>
    /// The single component of this type in the scene, for the one-of-a-kind things a game keeps on
    /// a dedicated entity — game state, the board, the camera. Null when there is none.
    /// </summary>
    public static TComponent? Single<TComponent>(this IScene? scene) where TComponent : class, IComponent
    {
        if (scene is null)
        {
            return null;
        }

        foreach (var (_, entity) in scene.Entities)
        {
            if (entity.IsEnabled && entity.TryGetComponent<TComponent>(out var component) && component.IsActive)
            {
                return component;
            }
        }

        return null;
    }
}
