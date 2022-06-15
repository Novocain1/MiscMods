#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
layout(location = 2) in vec4 colorIn;
layout(location = 3) in int flags;
layout(location = 5) in int colormapData;

uniform vec4 rgbaTint;
uniform vec3 rgbaAmbientIn;
uniform vec4 rgbaLightIn;
uniform vec4 rgbaGlowIn;
uniform vec4 rgbaFogIn;
uniform int extraGlow;
uniform float fogMinIn;
uniform float fogDensityIn;
uniform vec3 origin;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

uniform int dontWarpVertices;
uniform int addRenderFlags;
uniform float extraZOffset;

out vec2 uv;
out vec4 color;
out vec4 rgbaFog;
out vec4 rgbaGlow;
out float fogAmount;

out vec3 normal;
#if SSAOLEVEL > 0
out vec4 fragPosition;
out vec4 gnormal;
#endif


#include vertexflagbits.ash
#include shadowcoords.vsh
#include fogandlight.vsh
#include vertexwarp.vsh
#include colormap.vsh

void main(void)
{
	vec4 worldPos = modelMatrix * vec4(vertexPositionIn, 1.0);
	
	bool isLeaves = (((flags | addRenderFlags) & 0x8000000) > 0);
	if (dontWarpVertices == 0) {
		worldPos = applyVertexWarping(flags | addRenderFlags, worldPos);
	}
	
	vec4 camPos = viewMatrix * worldPos;
	
	uv = uvIn;
	int glow = min(255, extraGlow + (flags & GlowLevelBitMask));
	int renderFlags = glow | (flags & ~GlowLevelBitMask);
	rgbaGlow = rgbaGlowIn;
	
	color = rgbaTint * applyLight(rgbaAmbientIn, rgbaLightIn, renderFlags, camPos) * colorIn;
	color.rgb = mix(color.rgb, rgbaGlowIn.rgb, glow / 255.0 * rgbaGlowIn.a);

	// Distance fade out
	color.a *= clamp(20 * (1.10 - length(worldPos.xz) / viewDistance) - 5, -1, 1);

	rgbaFog = rgbaFogIn;
	gl_Position = projectionMatrix * camPos;
	calcShadowMapCoords(viewMatrix, worldPos);
	calcColorMapUvs(colormapData, vec4(vertexPositionIn + origin, 1.0) + vec4(playerpos, 1), rgbaLightIn.a, isLeaves);
	
	fogAmount = getFogLevel(worldPos, fogMinIn, fogDensityIn);
	
	gl_Position.w += extraZOffset;
	
	normal = unpackNormal(flags);
	normal = normalize((modelMatrix * vec4(normal.x, normal.y, normal.z, 0)).xyz);
	
	#if SSAOLEVEL > 0
		fragPosition = camPos;
		gnormal = viewMatrix * vec4(normal, 0);
	#endif
}