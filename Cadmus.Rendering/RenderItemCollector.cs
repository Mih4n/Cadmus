using System.Numerics;
using Cadmus.Core.Entities;
using Cadmus.Core.Scenes;
using Cadmus.Engine.Components.Sprites;
using Cadmus.Engine.Components;
using Cadmus.Engine.Geometry;

namespace Cadmus.Rendering;

/// <summary>One thing to draw: a mesh, a texture, a model matrix and a colour multiplier.</summary>
/// <param name="ScreenSpace">
/// When true the item ignores the scene camera and is placed in window pixels — used by the debug
/// overlay so a scrolling or zoomed camera cannot drag the HUD around.
/// </param>
public readonly record struct RenderItem(
    Mesh Mesh,
    string TexturePath,
    Matrix4x4 Model,
    Vector4 Tint,
    float Depth,
    bool ScreenSpace = false
);

/// <summary>
/// Turns a scene into a flat, depth-sorted draw list. Kept separate from the Vulkan renderer so the
/// "what to draw" rules stay testable without a GPU.
/// </summary>
public sealed class RenderItemCollector
{
    private readonly List<RenderItem> items = [];

    public IReadOnlyList<RenderItem> Collect(IScene? scene)
    {
        items.Clear();

        if (scene is null)
        {
            return items;
        }

        foreach (var (_, entity) in scene.Entities)
        {
            if (!entity.IsEnabled)
            {
                continue;
            }

            CollectSprites(entity);
            CollectMeshes(entity);
        }

        // Painter's order for equal depths; the depth buffer resolves the rest.
        items.Sort(static (left, right) => left.Depth.CompareTo(right.Depth));

        return items;
    }

    private void CollectSprites(IEntity entity)
    {
        var origin = entity.GetComponent<PositionComponent>()?.Vector ?? Vector3.Zero;

        foreach (var sprite in entity.GetComponents<SpriteComponent>())
        {
            if (!sprite.IsVisible)
            {
                continue;
            }

            var model = sprite.ComputeModelMatrix(origin);
            items.Add(
                new RenderItem(
                    sprite.Mesh,
                    sprite.TexturePath,
                    model,
                    sprite.Tint,
                    origin.Z + sprite.LocalPosition.Z
                )
            );
        }
    }

    private void CollectMeshes(IEntity entity)
    {
        if (!entity.TryGetComponent<MeshComponent>(out var mesh) ||
            !entity.TryGetComponent<MaterialComponent>(out var material))
        {
            return;
        }

        var transform = entity.GetComponent<TransformComponent>();
        var model = transform?.GetModelMatrix()
                 ?? Matrix4x4.CreateTranslation(entity.GetComponent<PositionComponent>()?.Vector ?? Vector3.Zero);

        items.Add(
            new RenderItem(
                mesh.Mesh,
                material.TexturePath,
                model,
                material.Tint,
                model.Translation.Z
            )
        );
    }
}
