using System.Numerics;
using Cadmus.Engine.Components;

namespace TestGame.Snake.Components;

/// <summary>
/// The play field. <see cref="Origin"/> is where its top-left corner sits in window pixels and is
/// recomputed by the presentation system whenever the window changes size.
/// </summary>
public sealed class BoardComponent : Component
{
    public Vector2 Origin { get; set; }
}
