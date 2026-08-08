using Cadmus.Core.Game;
using Cadmus.Core.Windowing;
using Cadmus.Engine.Extensions;
using Cadmus.Graphics.Extensions;
using Cadmus.Graphics.Vulkan;
using Cadmus.Graphics;
using Cadmus.Rendering.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.App;

/// <summary>
/// Entry point of a Cadmus application:
/// <code>
/// var builder = CadmusApplication.CreateBuilder();
/// builder.Services.AddScene&lt;MainScene&gt;("Main");
/// builder.UseGame&lt;SnakeGame&gt;();
/// using var app = builder.Build();
/// await app.RunAsync();
/// </code>
/// </summary>
public sealed class CadmusApplicationBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();

    internal CadmusApplicationBuilder()
    {
        Services.AddCadmusEngine();
        Services.AddCadmusGraphics();
        Services.AddCadmusRendering();
        Services.TryAddSingleton<IGameHost, GameHost>();
    }

    public CadmusApplicationBuilder UseGame<TGame>() where TGame : class, IGame
    {
        Services.AddSingleton<IGame, TGame>();
        return this;
    }

    public CadmusApplicationBuilder ConfigureWindow(Action<GameWindowOptions> configure)
    {
        Services.ConfigureWindow(configure);
        return this;
    }

    public CadmusApplicationBuilder ConfigureRenderer(Action<VulkanOptions> configure)
    {
        Services.ConfigureVulkan(configure);
        return this;
    }

    public CadmusApplication Build()
    {
        if (Services.All(d => d.ServiceType != typeof(IGame)))
        {
            throw new InvalidOperationException($"No game registered — call {nameof(UseGame)}<TGame>() first.");
        }

        var provider = Services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            }
        );

        return new CadmusApplication(provider);
    }
}

/// <summary>A built application: owns the container and runs the host.</summary>
public sealed class CadmusApplication(ServiceProvider services) : IAsyncDisposable
{
    public IServiceProvider Services => services;

    public static CadmusApplicationBuilder CreateBuilder() => new();

    public Task RunAsync(CancellationToken cancellationToken = default) =>
        services.GetRequiredService<IGameHost>().RunAsync(cancellationToken);

    public ValueTask DisposeAsync() => services.DisposeAsync();
}
