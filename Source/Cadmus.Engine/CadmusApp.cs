using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.Engine;

public sealed class CadmusApp(IServiceProvider services) : IDisposable
{
    public IServiceProvider Services { get; } = services;
    
    public static CadmusBuilder CreateBuilder() => new();

    public void Update(float deltaTime)
    {
        Services.GetRequiredService<SystemScheduler>().Update(deltaTime);
    }

    public void Dispose()
    {
        (Services as IDisposable)?.Dispose();
    }
}
