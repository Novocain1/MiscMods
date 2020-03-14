#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
layout(location = 2) in vec4 colorIn;
layout(location = 3) in int flags;

uniform vec4 rgbaTint;
uniform vec3 rgbaAmbientIn;
uniform vec4 rgbaLightIn;
uniform vec4 rgbaGlowIn;
uniform vec4 rgbaBlockIn;
uniform vec4 rgbaFogIn;
uniform int extraGlow;
uniform float fogMinIn;
uniform float fogDensityIn;

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

flat out int renderFlags;
out vec3 normal;
flat out vec3 flatNormal;


#include shadowcoords.vsh
#include fogandlight.vsh
#include vertexwarp.vsh

void main(void)
{
	vec4 worldPos = modelMatrix * vec4(vertexPositionIn, 1.0);
	
	if (dontWarpVertices == 0) {
		worldPos = applyVertexWarping(flags | addRenderFlags, worldPos);
	}
	
	vec4 camPos = viewMatrix * worldPos;
	
	uv = uvIn;
	int glow = min(255, extraGlow + (flags & 0xff));
	renderFlags = glow | (flags & ~0xff);
	rgbaGlow = rgbaGlowIn;
	
	color = rgbaTint * applyLight(rgbaAmbientIn, rgbaLightIn, colorIn * rgbaBlockIn, renderFlags, camPos);
	color.rgb = mix(color.rgb, rgbaGlowIn.rgb, glow / 255.0 * rgbaGlowIn.a);
		
	rgbaFog = rgbaFogIn;
	gl_Position = projectionMatrix * camPos;
	calcShadowMapCoords(viewMatrix, worldPos);
	
	fogAmount = getFogLevel(worldPos, fogMinIn, fogDensityIn);
	
	gl_Position.w += extraZOffset;
	
	normal = unpackNormal(flags >> 15);
	flatNormal = normalize((modelMatrix * vec4(normal.x, normal.y, normal.z, 0)).xyz);
}