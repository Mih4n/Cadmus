#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec2 fragUV;
layout(location = 0) out vec4 outputColor;

layout(binding = 1) uniform sampler2D u_MainTex;

void main()
{
    vec4 tex = texture(u_MainTex, fragUV);
    outputColor = tex * vec4(fragColor, 1);
}
