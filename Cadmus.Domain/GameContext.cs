using Cadmus.Domain.Contracts;

namespace Cadmus.Domain;

public class GameContext(IGame game) : IGameContext
{
    public IGame Game => game;
    public IScene? Scene => game.CurrentScene;
}
