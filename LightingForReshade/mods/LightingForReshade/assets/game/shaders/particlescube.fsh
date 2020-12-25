#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

in vec4 color;
in vec2 uv;
in float glowLevel;
in float fogAmount;
in vec4 rgbaFog;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;

#include fogandlight.fsh

void main()
{
	outColor = applyFogAndShadow(color, fogAmount);
    
    float findBright = clamp(max(outColor.r, max(outColor.g, outColor.b)), 0, 0.25) - fogAmount;

    outGlow = vec4(glowLevel + findBright, 0, 0, outColor.a);
}

