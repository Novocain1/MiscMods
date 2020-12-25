#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

in vec4 rgbaCloud;
in vec4 rgbaFog;
in float fogAmountf;

in vec3 vertexPos;
flat in int flagsf;
in float thinCloudModef;

uniform float fogDensityIn;
uniform float fogMinIn;
uniform vec3 sunPosition;

layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;

#include noise3d.ash
#include dither.fsh
#include fogandlight.fsh
#include skycolor.fsh

void drawPixel(vec4 color, float glow) {
	float weight = color.a * clamp(0.03 / (1e-5 + pow(gl_FragCoord.z / 200, 4.0)), 1e-2, 3e3);
	
    // RGBA32F texture (accumulation)
    outAccu = vec4(color.rgb * color.a, color.a) * weight/3;

    // R32F texture (revealage)
    // Make sure to use the red channel (and GL_RED target in your texture)
    outReveal.r = color.a;
	
	float findBright = clamp(max(color.r, max(color.g, color.b)), 0, 0.25) - fogAmountf;
	
	outGlow = vec4(glow + findBright, 0, 0, min(1, color.a*5 - (flagsf >= 5 ? thinCloudModef : 0)));
}

void main()
{
	/*float a = (fragWorldPos.x)/20;
	float b = (fragWorldPos.z)/20;
	float noise = (cnoise(vec3(a * 0.4, b * 0.4, 1))/2 + cnoise(vec3(a, b, 1))/2 + 0.5) / 2;
	vec4 outColor = rgbaCloud;
	outColor.a *= 1 + noise;
	drawPixel(outColor);*/
	
	
	float sealevelOffsetFactor = 0.25;
	float dayLight = 1;
	float horizonFog = 0;
	// Due to earth curvature the clouds are actually lower, so we do +100 to not have them dismissed during sunglow coloring
	vec4 skyGlow = getSkyGlowAt(vec3(vertexPos.x, vertexPos.y+100, vertexPos.z), sunPosition, sealevelOffsetFactor, clamp(dayLight, 0, 1), horizonFog, 0.7);
	
	vec4 col = rgbaCloud;
	
	col.rgb *= mix(vec3(1), 1.2 * skyGlow.rgb, skyGlow.a);
	col.rgb *= max(1, 0.9 + skyGlow.a/10);
	
	col.rgb = mix(col.rgb, rgbaFog.rgb, fogAmountf);
	
	// Seems to give a ~8 FPS boost on an intel hd 620 when looking at the sky at 128 view distance
	if (col.a < 0.005) discard;
	
	drawPixel(col, max(0, skyGlow.a/10 - 0.1));
}
