using Cadmus.Domain.Contracts;
using Cadmus.Domain.Contracts.Components;
using Cadmus.Domain.Contracts.Game;

namespace Cadmus.Domain.Game;

public class GameContext(IGame game) : IGameContext
{
    public IGame Game => game;
    public IScene? Scene => game.CurrentScene;
}
