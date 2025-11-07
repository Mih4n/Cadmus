using System.Numerics;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;
using Cadmus.Render;
using Cadmus.Render.Camera;
using Cadmus.Render.Rendering;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

public sealed class VulkanRenderer : ISystem, IDisposable
{
    private Sdl2Window window;
    private CommandList commands;
    private GraphicsDevice device;

    private Camera2D camera;
    private RenderPipeline pipeline;
    private VulkanRenderBackend backend;

    public VulkanRenderer()
    {
        var windowCI = new WindowCreateInfo { WindowInitialState = WindowState.FullScreen, X = 0, Y = 0, WindowTitle = "Cadmus" };
        window = VeldridStartup.CreateWindow(ref windowCI);
        var options = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved, true);
        device = VeldridStartup.CreateVulkanGraphicsDevice(options, window);
        commands = device.ResourceFactory.CreateCommandList();

        camera = new Camera2D { ViewportWidth = 800, ViewportHeight = 600, Position = new Vector2(400, 300) };

        backend = new VulkanRenderBackend(device, commands);

        pipeline = new RenderPipeline(camera, backend);
    }

    public Task Update(IGameContext context)
    {
        window.PumpEvents();
        commands.Begin();
        commands.SetFramebuffer(device.SwapchainFramebuffer);
        commands.ClearColorTarget(0, new RgbaFloat(0.2f, 0.4f, 1.0f, 1.0f));
        // commands.ClearDepthStencil(1f);

        // Here: fill pipeline with sprites from the game world.
        // For demonstration, you might have game provide sprites via context or have a test submission.
        // But pipeline.BeginFrame/EndFrame will now use backend.DrawSprite which records draws to commandList.

        // IMPORTANT: Veldrid expects commands recorded with the same CommandList instance that is used here.
        // Our backend.DrawSprite uses the `cl` reference given at Initialize and calls DrawIndexed on it.
        // Make sure backend was initialized with the same commandList instance.

        pipeline.BeginFrame();

        // --- demo: create a simple sprite and submit (in real engine sprites come from scenes/entities)
        // (This block is optional and only for quick demo testing; remove in production)
        
        var shader = new Cadmus.Render.Rendering.Shader() { Name = "Test" };
        var material = new Material(shader);
        var quad = Mesh.CreateUnitQuad();
        var sprite = new Sprite(quad, material)
        {
            Position = new Vector3(200, 200, 0),
            Scale = new Vector2(64, 64)
        };
        pipeline.SubmitSprite(sprite);

        pipeline.EndFrame();

        commands.End();
        device.SubmitCommands(commands);
        device.SwapBuffers();
        device.WaitForIdle();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        backend?.Dispose();
        commands?.Dispose();
        device?.Dispose();
    }
}

