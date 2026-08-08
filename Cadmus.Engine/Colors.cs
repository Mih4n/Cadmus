using System.Numerics;

namespace Cadmus.Engine;

/// <summary>
/// Colour helpers. Tints are uploaded to the shader as-is and the swapchain is an sRGB format, so
/// the GPU converts on write — meaning shader values are <em>linear</em>. Authoring colours by eye
/// (hex, 0-255) therefore needs the conversion these helpers perform, or everything looks washed out.
/// </summary>
public static class Colors
{
    public static readonly Vector4 White = Vector4.One;
    public static readonly Vector4 Transparent = Vector4.Zero;

    /// <summary>Builds a linear-space tint from familiar 0-255 sRGB channels.</summary>
    public static Vector4 FromSrgb(byte red, byte green, byte blue, byte alpha = 255) => new(
        SrgbToLinear(red / 255f),
        SrgbToLinear(green / 255f),
        SrgbToLinear(blue / 255f),
        alpha / 255f
    );

    /// <summary>Builds a linear-space tint from an <c>0xRRGGBB</c> or <c>0xAARRGGBB</c> literal.</summary>
    public static Vector4 FromHex(uint value)
    {
        var alpha = (value & 0xFF000000) != 0 ? (byte)(value >> 24) : (byte)255;

        return FromSrgb(
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value,
            alpha
        );
    }

    private static float SrgbToLinear(float channel) => channel <= 0.04045f
        ? channel / 12.92f
        : MathF.Pow((channel + 0.055f) / 1.055f, 2.4f);
}
