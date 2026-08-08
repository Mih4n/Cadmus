#version 450

layout(location = 0) in vec2 fragUV;
layout(location = 0) out vec4 outputColor;

// Same block as in sprite.vert; the layout must match exactly.
layout(set = 0, std140, binding = 0) uniform Matrices
{
    mat4 u_ViewProj;
    mat4 u_Model;
    vec4 u_Tint;
};

layout(set = 1, binding = 0) uniform sampler2D u_MainTex;

void main()
{
    outputColor = texture(u_MainTex, fragUV) * u_Tint;
}
