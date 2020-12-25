#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;
#if SSAOLEVEL > 0
in vec4 fragPosition;
in vec4 gnormal;
layout(location = 2) out vec4 outGNormal;
layout(location = 3) out vec4 outGPosition;
#endif


uniform sampler2D itemTex;
uniform float alphaTest = 0.001;

// Texture overlay "hack"
// We only have the base texture UV coordinates, which, for blocks and items in inventory is the block or item texture atlas, but none uv coords for a dedicated overlay texture
// So lets remove the base offset (baseUvOrigin) and rescale the coords (baseTextureSize / overlayTextureSize) to get useful UV coordinates for the overlay texture
uniform sampler2D tex2dOverlay;
uniform float overlayOpacity;
uniform vec2 overlayTextureSize;
uniform vec2 baseTextureSize;
uniform vec2 baseUvOrigin;
uniform int normalShaded;

in vec2 uv;
in vec4 color;
in float glowLevel;
in float n;
flat in int renderFlags;
in vec3 normal;
in vec3 vertexPosition;

#include normalshading.fsh

void main () {

	if (overlayOpacity > 0) {
		vec2 uvOverlay = (uv - baseUvOrigin) * (baseTextureSize / overlayTextureSize);
	
		vec4 col1 = texture(tex2dOverlay, uvOverlay);
		vec4 col2 = texture(itemTex, uv);
		
		float a1 = overlayOpacity * col1.a  * min(1, col2.a * 100);
		float a2 = col2.a * (1 - a1);
		
		outColor = vec4(
			(a1 * col1.r + col2.r * a2) / (a1+a2),
			(a1 * col1.b + col2.g * a2) / (a1+a2),
			(a1 * col1.g + col2.b * a2) / (a1+a2),
			a1 + a2
		) * color;
		
	} else {
		outColor = texture(itemTex, uv) * color;
	}

	outColor.a = clamp(outColor.a, 0, 1); // No idea why, makes held torches glitchy without

#if BLOOM == 0
	outColor.rgb *= 1 + glowLevel;
#endif

	// Ensure held item always being in the front
	gl_FragDepth = gl_FragCoord.z / 20;
	
	if (outColor.a < alphaTest) discard;
	
	if (normalShaded > 0) {
		float b = min(1, getBrightnessFromNormal(normal, 1, 0.45) + glowLevel);
		outColor *= vec4(b, b, b, 1);
	}
	
#if SSAOLEVEL > 0
	outGPosition = vec4(1);
	outGNormal = gnormal;
#endif
	float findBright = clamp(max(outColor.r, max(outColor.g, outColor.b)), 0, 0.25);

	outGlow = vec4(glowLevel + findBright, 0, 0, outColor.a);
}