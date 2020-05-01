#version 330 core
//#include dither.fsh

precision highp float;

in vec2 uv;
out vec4 outColor;

uniform sampler2D iDepthBuffer;
uniform sampler2D iColor;
uniform sampler2D iLight;
uniform sampler2D iGodrays;
uniform sampler2D iShadowMapNear;
uniform sampler2D iShadowMapFar;

uniform vec2 iResolution;
uniform vec2 iMouse;
uniform vec2 iCamera;
uniform vec3 iSunPos;
uniform vec3 iMoonPos;
uniform vec3 iPlayerPosition;
uniform vec3 iLookBlockPos;
uniform vec3 iLookEntityPos;
uniform vec3 iCameraPos;

//0 or 1
uniform vec4 iControls1; //Backward, Down, FloorSitting, Forward
uniform vec4 iControls2; //Jump, Left, LeftMouseDown, Right
uniform vec4 iControls3; //RightMouseDown, Sitting, Sneak, Sprint
uniform vec2 iControls4; //TriesToMove, Up

//some float or other
uniform float iMoonPhase;
uniform float iTime;
uniform float iTemperature;
uniform float iRainfall;
uniform float iCurrentHealth;
uniform float iMaxHealth;

//scalar
uniform float iTempScalar;

//By ID, if it's null, it'll be -1
uniform float iActiveItem;
uniform float iLookingAtBlock;
uniform float iLookingAtEntity;

vec3 red = vec3(1, 0, 0);
vec3 yellow = vec3(1, 1, 0);
vec3 blue = vec3(0, 0, 1);

//Current Enum Tool Type
//NoTool: -1, Knife: 0, Pickaxe: 1, Axe: 2, Sword: 3, Shovel: 4, Hammer: 5, Spear: 6, Bow: 7, Shears: 8, Sickle: 9, Hoe: 10, Saw: 11, Chisel: 12,
uniform float iActiveTool;

float rng = fract(sin(dot(vec2(uv.x, uv.y), vec2(12.9898, 78.233)))*43758.5453123);

vec3 Phosphors(vec3 orig)
{
	vec3 masks = vec3(sin(uv.x * iResolution.x * 0.5), sin((uv.x - 0.1) * iResolution.x * 0.5), sin((uv.x - 0.2) * iResolution.x * 0.5));
	masks *= orig;
	float lines = abs(sin((uv.y * iResolution.y * 0.5) + (uv.x * iResolution.x * 0.05)));
	masks -= lines;
	masks *= 3;
	return reflect(masks, orig);
}

vec4 CRT()
{
	vec3 col = texture(iColor, uv).rgb;

    return vec4(Phosphors(col), 1.0);
}

void main() 
{
	//if (uv.x < abs(sin(iTime * 0.1))) discard;
	//outColor = CRT();
	//outColor = vec4(Color.rgb, 1);
}