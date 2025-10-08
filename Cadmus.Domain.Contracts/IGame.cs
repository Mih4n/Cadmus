namespace Cadmus.Domain.Contracts;

public interface IGame : ISystem
{
    IScene CurrentScene { get; }
}
