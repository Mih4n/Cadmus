using System.Numerics;
using Cadmus.Engine.Components;
using Cadmus.Engine.Components.Sprites;
using Cadmus.Engine.Entities;

namespace TestGame.Snake;

/// <summary>The apple. One cell, one sprite.</summary>
public sealed class FoodEntity : Entity
{
    private const string Texture = "Assets/Textures/white.png";

    private readonly SnakeSettings settings;
    private readonly SpriteComponent sprite;

    public Cell Cell { get; private set; }

    public FoodEntity(SnakeSettings settings) : base("Food")
    {
        this.settings = settings;

        sprite = new SpriteComponent(
            Texture,
            settings.SpriteSize,
            new PositionComponent()
        )
        {
            Tint = settings.FoodColor
        };

        AddComponent(new PositionComponent());
        AddComponent(sprite);
    }

    public void MoveTo(Cell cell)
    {
        Cell = cell;
        sprite.GetComponent<PositionComponent>()!.Vector = settings.CellCenter(cell, 1f);
    }

    public void SyncSprites(Vector2 boardOrigin) =>
        GetComponent<PositionComponent>()!.Vector = new Vector3(boardOrigin, 0);
}
