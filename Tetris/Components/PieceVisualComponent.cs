using System.Numerics;
using Cadmus.Engine.Components;

namespace Tetris.Components;

/// <summary>
/// How far the drawn piece still lags behind the logical one, in cells and radians. A sideways move
/// or a wall kick records the jump it just made here and the smoothing system decays it to zero,
/// which is what turns a grid step into a glide. The fall carries no offset: it is already continuous
/// through <see cref="PieceComponent.FallProgress"/>.
/// </summary>
public sealed class PieceVisualComponent : Component
{
    public Vector2 Offset { get; set; }
    public float AngleOffset { get; set; }

    /// <summary>Records a move so the sprite starts from where it was drawn a frame ago.</summary>
    public void Displace(Cell from, Cell to) =>
        Offset += new Vector2(from.X - to.X, from.Y - to.Y);

    /// <summary>
    /// Records a rotation the same way. The angle is taken the short way round, so turning from the
    /// last state back to the first spins on a quarter turn instead of unwinding three.
    /// </summary>
    public void Turn(int from, int to) =>
        AngleOffset += ShortestAngle((from - to) * MathF.PI / 2f);

    /// <summary>Drops the lag, for a hard drop or a fresh piece that should simply appear.</summary>
    public void Snap()
    {
        Offset = Vector2.Zero;
        AngleOffset = 0f;
    }

    private static float ShortestAngle(float angle) => (float)Math.IEEERemainder(angle, Math.Tau);
}
