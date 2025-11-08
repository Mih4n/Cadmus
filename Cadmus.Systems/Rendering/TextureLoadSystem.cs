using Cadmus.Domain.Components.Rendering;
using Cadmus.Domain.Components.Sprites;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;
using Veldrid;
using Veldrid.ImageSharp;

namespace Cadmus.Systems.Rendering;

public class TextureLoadSystem : ISystem
{
    private static string fallbackTexturePath = "Assets/Textures/fallback.png";
    private Texture fallbackTexture;

    public TextureLoadSystem(IGameContext gameContext)
    {
        var context = gameContext.Game.GetComponent<VulkanRenderingContextComponent>() ?? throw new Exception();

        var device = context.Device;
        var factory = device.ResourceFactory;

        var path = Path.Combine(AppContext.BaseDirectory, fallbackTexturePath);
        var image = new ImageSharpTexture(path); 
        fallbackTexture = image.CreateDeviceTexture(device, factory);
    }

    public Task Update(IGameContext gameContext)
    {
        var scene = gameContext.Scene;
        if (scene is null) return Task.CompletedTask;

        var context = gameContext.Game.GetComponent<VulkanRenderingContextComponent>() ?? throw new Exception();

        var device = context.Device;
        var factory = device.ResourceFactory;

        foreach (var (_, entity) in scene.Entities)
        {
            if (!entity.HasComponent<SpriteComponent>()) continue;

            var sprites = entity.GetComponents<SpriteComponent>();
            sprites = sprites.Where(s => !s.Loaded);

            foreach (var sprite in sprites)
            {
                var path = Path.Combine(AppContext.BaseDirectory, sprite.Path);
                if (!File.Exists(path))
                {
                    sprite.Texture = fallbackTexture;
                    continue;
                }     

                var image = new ImageSharpTexture(path);
                var texture = image.CreateDeviceTexture(device, factory);

                sprite.Loaded = true;
                sprite.Texture = texture; 
            }
        }

        return Task.CompletedTask;
    }
}