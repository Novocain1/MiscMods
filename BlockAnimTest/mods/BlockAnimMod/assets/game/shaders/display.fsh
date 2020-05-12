#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;

uniform sampler2D tex;
uniform float extraGodray = 0;
uniform float alphaTest = 0.001;


// Texture overlay "hack"
// We only have the base texture UV coordinates, which, for blocks and items in inventory is the block or item texture atlas, but none uv coords for a dedicated overlay texture
// So lets remove the base offset (baseUvOrigin) and rescale the coords (baseTextureSize / overlayTextureSize) to get useful UV coordinates for the overlay texture
uniform sampler2D tex2dOverlay;

uniform sampler2D screenColors;
uniform vec2 resolution;

uniform float overlayOpacity;
uniform float brightness;
uniform vec2 overlayTextureSize;
uniform vec2 baseTextureSize;
uniform vec2 baseUvOrigin;
uniform int normalShaded;

in vec2 uv;
in vec4 color;
in vec4 rgbaFog;
in float fogAmount;
in float glowLevel;
in vec4 rgbaGlow;
flat in int renderFlags;
flat in vec3 normal;



#include fogandlight.fsh

void main () {
	outColor = texture(screenColors, uv);
	float incGlow = glowLevel + (outColor.r + outColor.g + outColor.b) / 3.0 * brightness;
	outGlow = vec4(incGlow, extraGodray - fogAmount, 0, outColor.a);
}