#version 450

layout(location = 0) in vec2 position;
layout(location = 1) in vec3 color;
layout(location = 2) in vec2 uv;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec2 fragUV;

layout(std140, binding = 0) uniform Matrices
{
    mat4 u_ViewProj;
    mat4 u_Model;
};

void main()
{
    fragColor = color;
    fragUV = uv;
    gl_Position = u_ViewProj * u_Model * vec4(position, 0, 1);
}

