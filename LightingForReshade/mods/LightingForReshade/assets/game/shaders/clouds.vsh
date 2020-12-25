#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

// Per vertex attribute
layout(location = 0) in vec3 vertexPosition;	//Per vertex
layout(location = 1) in vec4 rgbaBase;			// Per vertex
layout(location = 2) in int flags;					// Per vertex

// custom shorts, all normalized
layout(location = 3) in vec2 cloudTileOffset;			// Per instance. 
layout(location = 4) in vec4 neibCloudThickness;	// Per instance
layout(location = 5) in float selfThickness;			// Per instance
layout(location = 6) in float cloudBrightness;		// Per instance
layout(location = 7) in float thinCloudModeness;	// Per instance
layout(location = 8) in float undulatingModeness;	// Per instance
layout(location = 9) in float cloudOpaqueness;		// Per instance



// Per drawcall attribute
uniform vec3 sunPosition;
uniform vec3 sunColor;
uniform float dayLight;
uniform vec3 windOffset;
uniform vec4 rgbaFogIn;
uniform vec3 playerPos;
uniform float fogMinIn;
uniform float fogDensityIn;
uniform float globalCloudBrightness;
uniform int cloudTileSize;
uniform float cloudsLength;

uniform float cloudCounter;
uniform float cloudYTranslate; // Already added to the modelViewMatrix
uniform vec2 tileOffset;


uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;

out vec3 vertexPos;
flat out int flagsf;
out float thinCloudModef;

out vec4 rgbaCloud;
out vec4 rgbaFog;
out float fogAmountf;

#include colorutil.ash
#include noise3d.ash
#include shadowcoords.vsh
#include fogandlight.vsh


void main()
{
	float heightmul = (500 - 500 * thinCloudModeness);
    thinCloudModef = thinCloudModeness;
	float extraHeight = pow(selfThickness - 0.1, 2) * heightmul;
	float neibExtraHeight = pow(neibCloudThickness[flags] - 0.1, 2) * heightmul;
	
	float shortMaxValue = 32767.0; // Because cloudTileOffset is normalized
	vec3 vexPos = vertexPosition + vec3(shortMaxValue * cloudTileOffset.x, 0, shortMaxValue * cloudTileOffset.y);

	// Dense clouds are taller
	if (vexPos.y > 0) vexPos.y += extraHeight * max(0, 1 - 3*thinCloudModeness);
	
	float aMul = 1.0;
	flagsf = flags;

	if (flags < 4) {
		//yPos = vexPos.y + cloudTileSize/4;  // => goes from 0 ... cloudTileSize/2 + extraHeight

		float thicknessDiff = abs(selfThickness - neibCloudThickness[flags]);
		
		// This seems to look good for all the cloud patterns
		// No need to do any side cutoff, just fade them
		aMul = clamp(
			thicknessDiff 
			+ max(0, (1 - neibCloudThickness[flags]) * (1 -thinCloudModeness))
			+ max(0, (selfThickness - 0.2) * (1-thinCloudModeness))
		, 0, 1);
		
		if (neibCloudThickness[flags] <= 0.001) {
			aMul = 1;
		}
	}
	
	// Fade out distant clouds
	float distToEdge = cloudsLength/2 - length(vexPos);
	float distFade = min(1, distToEdge/(cloudsLength * 0.2));
	
	
	float undulate = gnoise(vec3((vexPos.x + tileOffset.x)/100.0, (vexPos.z + tileOffset.y)/500.0, cloudCounter)) * undulatingModeness;
	undulate *= min(1, (distToEdge + 400)/cloudsLength);
	vexPos.y += undulate*50;
	

	// Add "earth" curvature
	vexPos.y -= max(length(vexPos.xz + windOffset.xz) / (10/*+10*thinCloudModeness - what for?*/) - 50, 0);

	vec3 Sn = normalize(sunPosition);	
	
	vertexPos = vexPos.xyz;
	
	gl_Position = projectionMatrix * modelViewMatrix * vec4(vexPos, 1.0);
	float fogAmount = getFogLevel(vec4(vexPos.x, vexPos.y + cloudYTranslate, vexPos.z, 1), fogMinIn, fogDensityIn);
	
	rgbaCloud = vec4(rgbaBase.rgb, min(1, cloudOpaqueness * min(1, 10*selfThickness)));
	
	// Adjust brightness
	rgbaCloud.rgb *= dayLight * cloudBrightness * globalCloudBrightness;
	
	// Blend to fog color
	//rgbaCloud.rgb = mix(vertexColor.rgb, rgbaFogIn.rgb, min(1, fogAmount + (1-dayLight)));
	rgbaFog = rgbaFogIn;
	fogAmountf = clamp(fogAmount + clamp(1 - 4 * dayLight, -0.04, 1), 0, 1);
	//fogAmountf = clamp(fogAmount + dayLight, 0, 1);
	

	rgbaCloud.rgb *= mix(1, 0.2 + 0.75*(undulate + 0.5), undulatingModeness);
	rgbaCloud.a *= max(0, aMul * distFade);
	rgbaCloud.a += undulatingModeness/2;
	
	if (flags==4) rgbaCloud.a *= 0.75;
	if (flags==5) rgbaCloud.a *= 1.25;
	
	// Desired behavior
	//if (flags < 4 && neibCloudThickness[flags] > 0 && abs(neibCloudThickness[flags] - selfThickness) < 0.01 + thinCloudModeness/10) rgbaCloud.a=0;
	
	// Desired behavior - faded
	if (flags < 4) {
		float sideFade = max(
			1 - 50 * neibCloudThickness[flags],
			20 * abs(neibCloudThickness[flags] - selfThickness) // the thickness tests here should probably take thinCloudModeness into account
		);
		rgbaCloud.a *= clamp(sideFade, 0, 1);
	}
	
	rgbaCloud.a = clamp(rgbaCloud.a, 0, 1);
}
