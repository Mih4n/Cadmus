using Cadmus.Domain;
using Cadmus.Domain.Contracts;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

public sealed class VulkanRenderer : ISystem, IDisposable
{
    private Sdl2Window? window;
    private CommandList? commandList;
    private GraphicsDevice? graphicsDevice;

    private bool initialized;

    public void Initialize()
    {
        if (initialized) return;

        var windowCI = new WindowCreateInfo
        {
            X = 50,
            Y = 50,
            WindowWidth = 800,
            WindowHeight = 600,
            WindowTitle = "Cadmus + Vulkan (Veldrid)"
        };

        window = VeldridStartup.CreateWindow(ref windowCI);

        var options = new GraphicsDeviceOptions(
            debug: false,
            swapchainDepthFormat: null,
            syncToVerticalBlank: true,
            resourceBindingModel: ResourceBindingModel.Improved
        );

        graphicsDevice = VeldridStartup.CreateVulkanGraphicsDevice(
            options,
            window
        );

        commandList = graphicsDevice.ResourceFactory.CreateCommandList();

        initialized = true;
    }

    public Task Update(IGameContext context)
    {
        if (!initialized)
            Initialize();

        if (window is null) return Task.CompletedTask;
        if (commandList is null) return Task.CompletedTask;
        if (graphicsDevice is null) return Task.CompletedTask;

        if (!window.Exists)
            return Task.CompletedTask;

        // process window input / close events
        window.PumpEvents();

        commandList.Begin();
        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, new RgbaFloat(0.2f, 0.4f, 1.0f, 1.0f)); // чёрный фон

        // Тут в будущем: SetPipeline(...) + Draw(...)

        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.SwapBuffers();
        graphicsDevice.WaitForIdle();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        commandList?.Dispose();
        graphicsDevice?.Dispose();
        window?.Close();
    }
}
