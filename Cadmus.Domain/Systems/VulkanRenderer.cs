using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;
using Cadmus.Render;
using Cadmus.Render.Camera;
using Cadmus.Render.Rendering;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp; 
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Cadmus.Domain.Systems;

public sealed class VulkanRenderer : ISystem, IDisposable
{
    private Sdl2Window window;
    private CommandList commands;
    private GraphicsDevice device;
    private Camera2D camera;
    private RenderPipeline pipeline;
    private VulkanRenderBackend backend;

    private Sprite testSprite; 
    
    // --- НОВОЕ: Поле для хранения текстуры ---
    private Texture testTexture;

    public VulkanRenderer()
    {
        var windowCI = new WindowCreateInfo { WindowInitialState = WindowState.Normal, X = 100, Y = 100, WindowWidth = 800, WindowHeight = 600, WindowTitle = "Cadmus" };
        window = VeldridStartup.CreateWindow(ref windowCI);
        var options = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved, true);
        device = VeldridStartup.CreateVulkanGraphicsDevice(options, window);
        commands = device.ResourceFactory.CreateCommandList();
        camera = new Camera2D { ViewportWidth = 800, ViewportHeight = 600, Position = new Vector2(400, 300) };
        

        var factory = device.ResourceFactory;
        
        backend = new VulkanRenderBackend(device, commands);
        pipeline = new RenderPipeline(camera, backend);

        // --- Загрузка текстуры ---
        // Убедитесь, что файл "my_texture.png" существует и копируется в выходную директорию
        var imagePath = Path.Combine(AppContext.BaseDirectory, "my_texture.png"); 
        if (!File.Exists(imagePath)) 
            throw new FileNotFoundException("Не могу найти текстуру! Убедитесь, что она есть и 'Копируется в выходную директорию'.", imagePath);
        
        var imgSharpTexture = new ImageSharpTexture(imagePath);
        testTexture = imgSharpTexture.CreateDeviceTexture(device, factory);
        // --- Конец загрузки ---


        // --- Создаем спрайт с НОВЫM МАТЕРИАЛОМ ---
        
        // 1. Создаем материал, который содержит нашу текстуру
        var material = new Material(testTexture); 
        
        // 2. Создаем меш (как и раньше)
        var quad = Mesh.CreateUnitQuad();
        
        // 3. Создаем спрайт
        testSprite = new Sprite(quad, material)
        {
            Position = new Vector3(400, 300, 0), // В центре
            Scale = new Vector2(128, 128) // Покрупнее
        };
    }

    public Task Update(IGameContext context)
    {
        if (window.Exists)
        {
            window.PumpEvents();
            commands.Begin();
            commands.SetFramebuffer(device.SwapchainFramebuffer);
            commands.ClearColorTarget(0, new RgbaFloat(0.2f, 0.4f, 1.0f, 1.0f));
            
            pipeline.BeginFrame();

            // Отправляем тот же спрайт каждый кадр
            pipeline.SubmitSprite(testSprite); 
            
            pipeline.EndFrame(); 

            commands.End();
            device.SubmitCommands(commands);
            device.SwapBuffers();
            device.WaitForIdle();
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // --- ОЧИСТКА ТЕКСТУРЫ ---
        testTexture.Dispose();

        pipeline.EndFrame(); 
        backend.Dispose();
        device.WaitForIdle();
        commands.Dispose();
        device.Dispose();
        window.Close();
    }
}
