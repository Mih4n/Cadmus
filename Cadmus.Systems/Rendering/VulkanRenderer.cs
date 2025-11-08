using Cadmus.Domain.Components.Rendering;
using Cadmus.Domain.Components.Sprites;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;
using Cadmus.Render;
using Cadmus.Render.Camera;
using Cadmus.Render.Rendering;
using System.Numerics;
using Veldrid;

namespace Cadmus.Systems.Rendering;

public sealed class VulkanRenderer : ISystem, IDisposable
{
    private Camera2D camera;
    private RenderPipeline pipeline;
    private VulkanRenderBackend backend;

    public VulkanRenderer(IGameContext context)
    {
        camera = new Camera2D { ViewportWidth = 800, ViewportHeight = 600, Position = new Vector2(400, 300) };
        
        backend = new VulkanRenderBackend(context);
        pipeline = new RenderPipeline(camera, backend);
    }
    
    public Task Update(IGameContext gameContext)
    {
        var context = gameContext.Game.GetComponent<VulkanRenderingContextComponent>();
        if (context is null) throw new Exception("no context");

        var window = context.Window;
        var device = context.Device;
        var commands = context.Commands;

        if (!window.Exists) return Task.CompletedTask;
        if (gameContext.Scene is null) return Task.CompletedTask; 

        var scene = gameContext.Scene;

        window.PumpEvents();
        commands.Begin();
        commands.SetFramebuffer(device.SwapchainFramebuffer);
        commands.ClearColorTarget(0, new RgbaFloat(0.2f, 0.4f, 1.0f, 1.0f));
        
        pipeline.BeginFrame();

        foreach (var (_, entity) in scene.Entities)
        {
            if (!entity.HasComponent<SpriteComponent>()) continue;

            foreach (var sprite in entity.GetComponents<SpriteComponent>())
            {
                if (sprite.Loaded)
                    pipeline.SubmitSprite(sprite);
            }
        }
        
        pipeline.EndFrame(); 

        commands.End();
        device.SubmitCommands(commands);
        device.SwapBuffers();
        device.WaitForIdle();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        pipeline.EndFrame(); 
        backend.Dispose();
    }
}
