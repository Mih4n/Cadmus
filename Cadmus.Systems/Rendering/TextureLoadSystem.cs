using Cadmus.Domain.Components.Rendering;
using Cadmus.Domain.Components.Sprites;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;

namespace Cadmus.Systems.Rendering;

public class TextureLoadSystem : ISystem
{
    private static string fallbackTexturePath = "Assets/Textures/fallback.png";

    public TextureLoadSystem(IGameContext gameContext)
    {
    }

    public Task Update(IGameContext gameContext)
    {
        var scene = gameContext.Scene;
        if (scene is null) return Task.CompletedTask;

        var context = gameContext.Game.GetComponent<VulkanRenderingContext>() ?? throw new Exception();

        foreach (var (_, entity) in scene.Entities)
        {
            if (!entity.HasComponent<SpriteComponent>()) continue;

            var sprites = entity.GetComponents<SpriteComponent>();
            sprites = sprites.Where(s => !s.Loaded);

            foreach (var sprite in sprites)
            {
                sprite.Loaded = true;

                var path = Path.Combine(AppContext.BaseDirectory, sprite.Path);
                if (!File.Exists(path))
                {
                    continue;
                }     

            }
        }

        return Task.CompletedTask;
    }
}