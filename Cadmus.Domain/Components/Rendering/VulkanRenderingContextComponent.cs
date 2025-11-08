using Cadmus.Domain.Contracts.Components;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Cadmus.Domain.Components.Rendering;

public class VulkanRenderingContextComponent : IComponent, IDisposable
{
    public Sdl2Window Window { get; set; }
    public CommandList Commands { get; set; }
    public GraphicsDevice Device { get; set; }

    public VulkanRenderingContextComponent()
    {
        var deviceOptions = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved, true);
        var windowCreateInfo = new WindowCreateInfo 
        { 
            WindowInitialState = WindowState.Normal, 
            X = 100, 
            Y = 100, 
            WindowWidth = 800, 
            WindowHeight = 600, 
            WindowTitle = "Cadmus" 
        };

        Window = VeldridStartup.CreateWindow(ref windowCreateInfo);
        Device = VeldridStartup.CreateVulkanGraphicsDevice(deviceOptions, Window);
        Commands = Device.ResourceFactory.CreateCommandList();
    }

    public VulkanRenderingContextComponent(Sdl2Window window, CommandList commands, GraphicsDevice device)
    {
        Window = window;
        Device = device;
        Commands = commands;
    }

    public void Dispose()
    {
        Device.WaitForIdle();
        Commands.Dispose();
        Device.Dispose();
        Window.Close();
    }
}
