#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

in vec2 uv;
in vec4 color;
in vec4 rgbaFog;
in float fogAmount;
in float glowLevel;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;
#if SSAOLEVEL > 0
in vec4 fragPosition;
in vec4 gnormal;
layout(location = 2) out vec4 outGNormal;
layout(location = 3) out vec4 outGPosition;
#endif


uniform sampler2D entityTex;
uniform float alphaTest = 0.001;
uniform float windWaveCounter;
uniform float glitchEffectStrength;

flat in int renderFlags;
in vec3 normal;

#include fogandlight.fsh
#include noise3d.ash

void main () {
	vec4 texColor = texture(entityTex, uv) * color;
	
	#if SHADOWQUALITY > 0
	float intensity = 0.34 + (1 - shadowIntensity)/8.0; // this was 0.45, which makes shadow acne visible on blocks
	#else
	float intensity = 0.45;
	#endif

	outColor = applyFogAndShadowWithNormal(texColor, fogAmount, normal, 1, intensity); // was 0.35. Made it match whats in chunkopaque.fsh so animated blocks don't change brightness

	//outColor.r = normal.x;
	//outColor.g = normal.y;
	//outColor.b = normal.z;
	//outColor = vec4((normal.x + 0.5) / 2, (normal.y + 0.5)/2, (normal.z+0.5)/2, 1);	
	
	if (glitchEffectStrength > 0) {
		float g = gnoise(vec3(gl_FragCoord.y / 2.0, gl_FragCoord.x / 2.0, windWaveCounter*30));
		outColor.a *= mix(1, clamp(0.7 + g / 2, 0, 1), glitchEffectStrength);
		
		float b = gnoise(vec3(0, 0, windWaveCounter*60));
		outColor.a *= mix(1, clamp(b * 10 + 2, 0, 1), glitchEffectStrength);
	}

	if (outColor.a < alphaTest) discard;

	float glow = 0;
#if SHINYEFFECT > 0	
	glow = pow(max(0, dot(normal, lightPosition)), 6) / 8 * shadowIntensity * (1 - fogAmount);
#endif

#if SSAOLEVEL > 0
	outGPosition = vec4(fragPosition.xyz, fogAmount + glowLevel);
	outGNormal = vec4(gnormal.xyz, 0);
#endif

	float findBright = clamp(max(outColor.r, max(outColor.g, outColor.b)), 0, 0.25) - fogAmount;

	outGlow = vec4(glowLevel + glow + findBright, 0, 0, color.a);
}