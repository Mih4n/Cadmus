
using Cadmus.Domain.Contracts.Components;

namespace Cadmus.Domain.Contracts.Game;

public interface IGameContext
{
    IGame Game { get; }
    IScene? Scene { get; }
}
