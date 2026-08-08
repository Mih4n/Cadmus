using Cadmus.Core.Game;

namespace Cadmus.Core.Systems;

/// <summary>
/// A system that also submits GPU work. <see cref="Render"/> is always called sequentially, after
/// every <see cref="ISystem.UpdateAsync"/> of the frame has completed.
/// </summary>
public interface IRenderSystem : ISystem
{
    void Render(GameTime time);
}
