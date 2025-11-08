using System.Numerics;
using Cadmus.Domain.Contracts.Components;

namespace Cadmus.Domain.Components;

public class PositionComponent(Vector3 vector) : IComponent
{
    public Vector3 Vector { get; set; } = vector;
    public float X { get => Vector.X; set => Vector = new Vector3(value, Vector.Y, Vector.Z); }
    public float Y { get => Vector.Y; set => Vector = new Vector3(Vector.X, value, Vector.Z); }
    public float Z { get => Vector.Z; set => Vector = new Vector3(Vector.X, Vector.Y, value); }

    public PositionComponent() : this(new Vector3(0)) {}
    public PositionComponent(Vector2 vector, float z) : this(new Vector3(vector, z)) {}
    public PositionComponent(float x, float y, float z) : this(new Vector3(x, y, z)) {}

    public void Translate(Vector3 translation) => Vector += translation;
    public void Translate(float x, float y, float z) => Vector += new Vector3(x, y, z);
    
    public static implicit operator Vector3(PositionComponent pos) => pos.Vector;
    public static implicit operator PositionComponent(Vector3 vector) => new(vector);
}
