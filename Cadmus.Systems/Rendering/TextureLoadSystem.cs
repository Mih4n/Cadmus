using Cadmus.Domain.Components.Sprites;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;

namespace Cadmus.Systems.Rendering;

public class TextureLoadSystem : ISystem
{
    public Task Update(IGameContext context)
    {
        var scene = context.Scene;
        if (scene is null) return Task.CompletedTask;

        foreach (var (_, entity) in scene.Entities)
        {
            if (!entity.HasComponent<SpriteComponent>()) continue;

            var sprites = entity.GetComponents<SpriteComponent>();
            sprites = sprites.Where(s => !s.Loaded);

            foreach (var sprite in sprites)
            {
                
            }
        }

        return Task.CompletedTask;
    }
}
