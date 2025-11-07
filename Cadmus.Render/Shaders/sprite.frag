#version 450

// layout(location = 0) in vec3 fragColor; // УБРАНО
layout(location = 0) in vec2 fragUV; // ИЗМЕНЕНО: location = 0
layout(location = 0) out vec4 outputColor;

// ИЗМЕНЕНО: Добавлен 'set = 1'
layout(set = 1, binding = 0) uniform sampler2D u_MainTex;

void main()
{
    vec4 tex = texture(u_MainTex, fragUV);
    // outputColor = tex * vec4(fragColor, 1); // СТАРАЯ ВЕРСИЯ
    outputColor = tex; // ИСПРАВЛЕНО
}
