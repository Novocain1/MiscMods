#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform sampler2D terrainTex;

in vec4 rgba;
in vec4 rgbaFog;
in float fogAmount;
in vec2 uv;
in float glowLevel;
in vec4 worldPos;
in vec3 blockLight;

in float normalShadeIntensity;
flat in int renderFlags;
flat in vec3 normal;


layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;

#include fogandlight.fsh
#include noise3d.ash
#include colormap.fsh

void drawPixel(vec4 color) {
	float weight = color.a * clamp(0.03 / (1e-5 + pow(gl_FragCoord.z / 200, 4.0)), 1e-2, 3e3);
	
    // RGBA32F texture (accumulation)
    outAccu = vec4(color.rgb * color.a, color.a) * weight;
	
    // R32F texture (revealage)
    // Make sure to use the red channel (and GL_RED target in your texture)
    outReveal.r = color.a;
	
	float findBright = clamp(max(color.r, max(color.g, color.b)), 0, 0.25) - fogAmount;
    outGlow = vec4(glowLevel + findBright, 0, 0, color.a);
}


void main() 
{
	// When looking through tinted glass you can clearly see the edges where we fade to sky color
	// Using this discard seems to completely fix that
	if (rgba.a < 0.005) discard;

	vec4 texColor = texture(terrainTex, uv) * rgba * getColorMapping(terrainTex);

	texColor = applyFogAndShadowWithNormal(texColor, fogAmount, normal, normalShadeIntensity, 0.45);

#if SHINYEFFECT > 0
	// Shiny bit flag
	if (((renderFlags >> 5) & 1) > 0) {
		vec3 worldVec = normalize(worldPos.xyz);
	
		float angle = 2 * dot(normalize(normal), worldVec);
		angle += gnoise(vec3(uv.x*500, uv.y*500, worldVec.z/10)) / 7.5;
		texColor.a = clamp(texColor.a + gnoise(vec3(2 * (worldVec.x/10 + angle), 2 * (worldVec.y/10 + angle), 2 * (worldVec.z/10 + angle)))/2, texColor.a/2, 1);
		texColor.rgb *= max(vec3(1), vec3(1) + 3*blockLight * gnoise(vec3(worldVec.x/10 + angle, worldVec.y/10 + angle, worldVec.z/10 + angle))/2);
	}
#endif	

	drawPixel(texColor);
}