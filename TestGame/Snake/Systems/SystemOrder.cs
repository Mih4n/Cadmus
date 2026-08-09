namespace TestGame.Snake.Systems;

/// <summary>
/// The game's frame order in one place. Event visibility follows it: a consumer must sit after its
/// publisher to see the event in the same frame.
/// </summary>
public static class SystemOrder
{
    public const int Input = 100;
    public const int Movement = 200;
    public const int Flow = 300;
    public const int Food = 400;
    public const int Presentation = 500;
}
