using Cadmus.Domain.Contracts.Game;

namespace Cadmus.Domain.Contracts.Systems;

public interface ISystem
{
    Task Update(IGameContext context);
}
