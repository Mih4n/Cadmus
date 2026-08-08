using System.Numerics;
using Cadmus.Core.Input;
using Cadmus.Engine.Components;
using Cadmus.Engine.Components.Sprites;
using Cadmus.Engine.Entities;

namespace TestGame.Snake;

/// <summary>
/// The snake: its cells, its heading, and the sprites that show them. The input service comes from
/// the container through the constructor — this is what <c>IEntityFactory</c> exists for.
/// </summary>
public sealed class SnakeEntity : Entity
{
    private const string Texture = "Assets/Textures/white.png";

    private readonly IInputService input;
    private readonly SnakeSettings settings;
    private readonly List<Cell> body = [];

    private Cell heading = Cell.Right;
    private Cell requestedHeading = Cell.Right;

    private int revision;
    private int spriteRevision = -1;
    private bool spriteDead;

    public SnakeEntity(IInputService input, SnakeSettings settings) : base("Snake")
    {
        this.input = input;
        this.settings = settings;

        AddComponent(new PositionComponent());
        Reset();
    }

    public IReadOnlyList<Cell> Body => body;
    public Cell Head => body[0];
    public int Length => body.Count;

    public void Reset()
    {
        body.Clear();

        var start = new Cell(settings.Columns / 4, settings.Rows / 2);
        for (int i = 0; i < settings.StartLength; i++)
        {
            body.Add(new Cell(start.X - i, start.Y));
        }

        heading = Cell.Right;
        requestedHeading = Cell.Right;
        revision++;
    }

    /// <summary>
    /// Records the latest direction request. Called every frame, while moves happen on the tick, so
    /// a quick double tap cannot turn the snake back into its own neck.
    /// </summary>
    public void ReadInput()
    {
        var requested = ReadDirection();

        if (requested is { } direction && !direction.IsOpposite(heading))
        {
            requestedHeading = direction;
        }
    }

    private Cell? ReadDirection()
    {
        if (input.WasKeyPressed(Key.Left) || input.WasKeyPressed(Key.A)) return Cell.Left;
        if (input.WasKeyPressed(Key.Right) || input.WasKeyPressed(Key.D)) return Cell.Right;
        if (input.WasKeyPressed(Key.Up) || input.WasKeyPressed(Key.W)) return Cell.Up;
        if (input.WasKeyPressed(Key.Down) || input.WasKeyPressed(Key.S)) return Cell.Down;

        return null;
    }

    /// <summary>Commits the direction requested since the last tick.</summary>
    public void ApplyTurn() => heading = requestedHeading;

    /// <summary>The cell the head will occupy on the next tick.</summary>
    public Cell NextHead => Head + heading;

    public void Advance(Cell head, bool grow)
    {
        body.Insert(0, head);

        if (!grow)
        {
            body.RemoveAt(body.Count - 1);
        }

        revision++;
    }

    /// <summary>
    /// Whether the snake covers <paramref name="cell"/>. The tail is excluded while moving, because
    /// it vacates its cell on the same tick the head enters it.
    /// </summary>
    public bool Occupies(Cell cell, bool ignoreTail = false)
    {
        var count = ignoreTail ? body.Count - 1 : body.Count;

        for (int i = 0; i < count; i++)
        {
            if (body[i] == cell)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Keeps the sprites in step with the body. The origin moves whenever the window resizes, but
    /// the segment sprites only need rebuilding when the snake actually changed.
    /// </summary>
    public void SyncSprites(Vector2 boardOrigin, bool isDead)
    {
        GetComponent<PositionComponent>()!.Vector = new Vector3(boardOrigin, 0);

        if (revision == spriteRevision && isDead == spriteDead)
        {
            return;
        }

        spriteRevision = revision;
        spriteDead = isDead;

        RemoveAllComponents<SpriteComponent>();

        for (int i = 0; i < body.Count; i++)
        {
            AddComponent(
                new SpriteComponent(
                    Texture,
                    settings.SpriteSize,
                    new PositionComponent(settings.CellCenter(body[i], 1f))
                )
                {
                    Tint = SegmentColor(i, isDead)
                }
            );
        }
    }

    private Vector4 SegmentColor(int index, bool isDead)
    {
        if (isDead)
        {
            return settings.DeadColor;
        }

        if (index == 0)
        {
            return settings.HeadColor;
        }

        // Fade from body to tail so the direction of travel reads at a glance.
        var t = body.Count <= 2 ? 0f : (index - 1) / (float)(body.Count - 2);

        return Vector4.Lerp(settings.BodyColor, settings.TailColor, t);
    }
}
