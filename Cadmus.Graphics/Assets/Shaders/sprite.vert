#version 450

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 uv;

layout(location = 0) out vec2 fragUV;

// Bound as a dynamic uniform buffer: one slot per draw, selected by a dynamic offset.
// Declared identically in sprite.frag — both stages read the same block.
layout(set = 0, std140, binding = 0) uniform Matrices
{
    mat4 u_ViewProj;
    mat4 u_Model;
    vec4 u_Tint;
};

void main()
{
    fragUV = uv;
    gl_Position = u_ViewProj * u_Model * vec4(position, 1);
}
