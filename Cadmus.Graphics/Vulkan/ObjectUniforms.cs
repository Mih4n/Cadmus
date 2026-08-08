using System.Numerics;
using System.Runtime.InteropServices;

namespace Cadmus.Graphics;

/// <summary>
/// Mirrors the <c>Matrices</c> block in sprite.vert. System.Numerics matrices are uploaded verbatim:
/// their row-major bytes are read back by GLSL as the transpose, which is exactly what
/// <c>u_ViewProj * u_Model * v</c> expects.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ObjectUniforms
{
    public Matrix4x4 ViewProjection;
    public Matrix4x4 Model;

    /// <summary>Multiplied into the sampled texel; std140 puts it at offset 128.</summary>
    public Vector4 Tint;
}
