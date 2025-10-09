namespace Cadmus.Domain.Contracts;

public interface IGameContext
{
    IGame Game { get; }
    IScene? Scene { get; }
}
