using System.Numerics;
using Cadmus.Engine.Components;

namespace Tetris.Components;

/// <summary>
/// The play field. <see cref="Origin"/> is where the well's top-left corner sits in window pixels and
/// is recomputed by the presentation system whenever the window changes size; every board-relative
/// entity is placed there, so the whole layout moves as one.
/// </summary>
public sealed class BoardComponent : Component
{
    public Vector2 Origin { get; set; }
}
