namespace Cadmus.Core.Input;

/// <summary>
/// Backend-agnostic key identity. Deliberately a small, explicit set rather than a mirror of the
/// backend's enum — the graphics layer maps these onto whatever the platform reports.
/// </summary>
public enum Key
{
    Unknown = 0,

    Left,
    Right,
    Up,
    Down,

    A,
    D,
    S,
    W,
    P,
    R,

    Space,
    Enter,
    Escape,

    F1,
    F3
}
