#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

in vec4 color;
in vec2 uv;
in float glowLevel;

layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;


uniform sampler2D particleTex;
uniform int oitPass;
uniform int withTexture;
uniform int heldItemMode;


void drawPixel(vec4 color) {
	float weight = color.a * clamp(0.03 / (1e-5 + pow(gl_FragCoord.z / 200, 4.0)), 1e-2, 3e3);
	
    // RGBA32F texture (accumulation)
    outAccu = vec4(color.rgb * color.a, color.a) * weight;

    // R32F texture (revealage)
    // Make sure to use the red channel (and GL_RED target in your texture)
    outReveal.r = color.a;
	
	float findBright = clamp(max(color.r, max(color.g, color.b)), 0, 0.25);

    outGlow = vec4(glowLevel, 0, 0, color.a);
}

void main()
{
	vec4 outColor;

	if (heldItemMode > 0) {
		// Ensure held item always being in the front
		gl_FragDepth = gl_FragCoord.z / 20;
	} else {
		gl_FragDepth = gl_FragCoord.z;
	}
	
	if (withTexture > 0) {
		outColor = color * texture(particleTex, uv);
	} else {
		outColor = color;
	}
	
	if (outColor.a < 0.002) discard;
	
	

	if (oitPass > 0) {
		// Dunno why but with this modifier the torch particles look more similar when held versus placed
		outColor.a*=1;
		
		drawPixel(outColor);
	} else {
		outAccu = outColor;
	}
}