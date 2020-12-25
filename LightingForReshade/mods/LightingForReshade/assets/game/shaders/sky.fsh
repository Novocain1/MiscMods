#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

in vec3 vertexPosition;
flat in vec4 rgbaFog;

uniform float fogDensityIn;
uniform float fogMinIn;
uniform float dayLight;
uniform float horizonFog;
uniform vec3 playerPos;
uniform vec3 sunPosition;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;
#if SSAOLEVEL > 0
layout(location = 2) out vec4 outGNormal;
layout(location = 3) out vec4 outGPosition;
#endif

#include dither.fsh
#include fogandlight.fsh
#include skycolor.fsh

void main()
{
	outColor = vec4(1);
	outGlow = vec4(1);
	float sealevelOffsetFactor = 0.25;
	getSkyColorAt(vertexPosition, sunPosition, sealevelOffsetFactor, clamp(dayLight, 0, 1), horizonFog, outColor, outGlow);
	
	outGlow.y *= clamp((dayLight - 0.05) * 2, 0, 1);
	
#if SSAOLEVEL > 0
	outGPosition = vec4(0);
	outGNormal = vec4(0);
#endif

}
