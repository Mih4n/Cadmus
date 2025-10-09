using Cadmus.Domain;
using Cadmus.Domain.Contracts;

namespace Cadmus.App.Systems.Base;

public class SpriteRender : ISystem
{
    public Task Update(IGameContext context)
    {
        return Task.CompletedTask;
    }
}
