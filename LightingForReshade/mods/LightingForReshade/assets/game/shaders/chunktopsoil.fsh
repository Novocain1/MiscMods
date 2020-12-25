#version 330 core

uniform sampler2D terrainTex;
uniform sampler2D terrainTexLinear;

uniform float alphaTest = 0.01;
uniform vec2 blockTextureSize;

in vec4 rgba;
in vec4 rgbaFog;
in float fogAmount;
in vec2 uv;
in vec2 uv2;
in float glowLevel;
in vec3 blockLight;
in vec3 vertexPosition;

flat in int renderFlags;
in vec3 normal;
in vec4 gnormal;



layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;
#if SSAOLEVEL > 0
in vec4 fragPosition;
layout(location = 2) out vec4 outGNormal;
layout(location = 3) out vec4 outGPosition;
#endif

#include fogandlight.fsh
#include colormap.fsh
#include noise3d.ash

void main() 
{
	vec4 brownSoilColor = texture(terrainTex, uv) * rgba;
	vec4 grassColor;
	
	if (normal.y < 0) {
		// Bottom
		outColor = applyFog(brownSoilColor, fogAmount);
	} else {
		if (normal.y > 0) {
			// Top
			grassColor = texture(terrainTex, uv2 + vec2(blockTextureSize.x, 0)) * getColorMapping(terrainTexLinear) * rgba;
		} else {
			// Side + Overlay
			grassColor = texture(terrainTex, uv2) * getColorMapping(terrainTexLinear) * rgba;
		}
		
		outColor = brownSoilColor * (1 - grassColor.a) + grassColor * grassColor.a;
	}
	

	#if SHADOWQUALITY > 0
	float intensity = 0.34 + (1 - shadowIntensity)/8.0; // this was 0.45, which makes shadow acne visible on blocks
	#else
	float intensity = 0.45;
	#endif
	
	outColor = applyFogAndShadowWithNormal(outColor, fogAmount, normal, 1, intensity);  
	outColor.a = rgbaFog.a;

	// When looking through tinted glass you can clearly see the edges where we fade to sky color
	// Using this discard seems to completely fix that
	if (outColor.a < 0.005) discard;


	float aTest = outColor.a;
	aTest += max(0, 1 - rgba.a) * min(1, outColor.a * 10);
	if (aTest < alphaTest) discard;
	
	float glow = 0;

#if SHINYEFFECT > 0
	// Shiny bit flag
	if (((renderFlags >> 5) & 1) > 0) {
		vec3 worldVec = normalize(vertexPosition.xyz);
	
		float angle = 2 * dot(normalize(normal), worldVec);
		angle += gnoise(vec3(uv.x*500, uv.y*500, worldVec.z/10)) / 7.5;		
		outColor.rgb *= max(vec3(1), vec3(1) + 3*blockLight * gnoise(vec3(worldVec.x/10 + angle, worldVec.y/10 + angle, worldVec.z/10 + angle)));
	}
	
	glow = pow(max(0, dot(normal, lightPosition)), 6) / 10 * shadowIntensity * (1 - fogAmount);
#endif	

	

#if SSAOLEVEL > 0
	outGPosition = vec4(fragPosition.xyz, fogAmount + glowLevel);
	outGNormal = gnormal;
#endif
	
	float findBright = clamp(max(outColor.r, max(outColor.g, outColor.b)), 0, 0.25) - fogAmount;
    outGlow = vec4(glowLevel + glow + findBright, 0, 0, outColor.a);
}