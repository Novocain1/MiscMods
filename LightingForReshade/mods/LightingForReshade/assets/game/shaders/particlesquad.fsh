#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

in vec4 color;
in vec2 uv;
in vec4 rgbaFog;
in vec3 vexPos;
in float fogAmount;
in float glowLevel;
in float extraWeight;


layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;

uniform sampler2D particleTex;

#include fogandlight.fsh

void drawPixel(vec4 color) {
	float weight = color.a * clamp(0.03 / (1e-5 + pow(gl_FragCoord.z / 200, 4.0)), 1e-2, 1e3);
	
    // RGBA32F texture (accumulation)
    outAccu = vec4(color.rgb * color.a, color.a) * (weight * extraWeight);

    // R32F texture (revealage)
    // Make sure to use the red channel (and GL_RED target in your texture)
    outReveal.r = color.a;	

    float findBright = clamp(max(color.r, max(color.g, color.b)), 0, 0.25) - fogAmount;
	
    outGlow = vec4(glowLevel + findBright, 0, 0, color.a);
}


void main()
{
	vec4 color = applyFog(color, fogAmount);
	//color=vec4(1);
	drawPixel(color);
}

