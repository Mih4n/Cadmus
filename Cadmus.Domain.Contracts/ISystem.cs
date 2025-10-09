using Cadmus.Domain.Contracts;

namespace Cadmus.Domain;

public interface ISystem
{
    Task Update(IGameContext context);
}
