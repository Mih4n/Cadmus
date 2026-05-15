using Cadmus.Domain.Components;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;
using Cadmus.Render;

namespace Cadmus.Systems.Rendering;

public class TextureLoadSystem : ISystem
{
    public TextureLoadSystem(IGameContext gameContext)
    {
    }

    public Task Update(IGameContext gameContext)
    {
        var scene = gameContext.Scene;
        if (scene is null) return Task.CompletedTask;

        var context = gameContext.Game.GetComponent<VulkanRenderingContext>() ?? throw new Exception("No VulkanRenderingContext found");

        foreach (var (_, entity) in scene.Entities)
        {
            if (!entity.HasComponent<MaterialComponent>()) continue;

            var materials = entity.GetComponents<MaterialComponent>();
            foreach (var material in materials)
            {
                if (material.GpuTexture != null) continue;

                var path = Path.Combine(AppContext.BaseDirectory, material.TexturePath);
                if (!File.Exists(path))
                {
                    continue;
                }

                material.GpuTexture = new VulkanTexture(context.Vulkan, context.Device!, path);
            }
        }

        return Task.CompletedTask;
    }
}
