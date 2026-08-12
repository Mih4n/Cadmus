namespace Tetris.Systems;

/// <summary>
/// The game's frame order in one place. Event visibility follows it: a consumer must sit after its
/// publisher to see the event in the same frame.
/// </summary>
public static class SystemOrder
{
    public const int Input = 100;
    public const int Control = 200;
    public const int Gravity = 300;
    public const int Well = 400;
    public const int Spawn = 500;
    public const int Flow = 600;
    public const int Smoothing = 700;
    public const int Presentation = 800;
    public const int Hud = 810;
    public const int Title = 900;
}
