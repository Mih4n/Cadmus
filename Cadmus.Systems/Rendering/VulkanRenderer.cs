using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;

namespace Cadmus.Systems.Rendering;

public sealed class VulkanRenderer : ISystem, IDisposable
{

    public VulkanRenderer(IGameContext context)
    {
    }
    
    public Task Update(IGameContext gameContext)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
