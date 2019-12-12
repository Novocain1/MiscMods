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

float Circle(vec2 uv2, vec2 p, float r, float blur){
	return smoothstep(r, r - blur, length(uv2 - p));
}

vec2 Rotate(float speedx, float speedy, float radius){
	return vec2(sin(iTime*speedx)*radius, cos(iTime*speedy)*radius);
}

float near = 0.5; 
float far  = 1.0;

float DepthAtPixel(vec2 pix) 
{
	float depth = texture(iDepthBuffer, pix).x;
    float z = depth * 2.0 - 1.0;
    return (2.0 * near * far) / (far + near - z * (far - near));
}

vec4 ColorAtPixel(vec2 pix)
{
	return texture(iColor, pix);
}

float Depth() 
{
	return DepthAtPixel(uv);
}

vec4 SNAtPixel(vec2 pix)
{
	return texture(iShadowMapNear, pix);
}

vec4 ShadowMapNear() 
{
	return SNAtPixel(uv);
}

vec4 SFAtPixel(vec2 pix)
{
	return texture(iShadowMapFar, pix);
}

vec4 ShadowMapFar() 
{
	return SFAtPixel(uv);
}


float depth = Depth();

vec4 Color = texture(iColor, uv);
vec4 Light = texture(iLight, uv);
float iGlow = Light.r;
float iGodRay = Light.g;

vec3 camVec = vec3(cos(iCamera.x) * cos(iCamera.y), sin(iCamera.x), cos(iCamera.x) * sin(-iCamera.y));
vec3 camNrm = normalize(camVec);

//vec3 Nrm = normalize(vec3(Depth(), 1.0 - Depth(), 1.0));
float rng = fract(sin(dot(vec2(uv.x, uv.y), vec2(12.9898, 78.233)))*43758.5453123);
const vec3 nrmavg = vec3(128.0/255.0, 128.0/255.0, 1);

float Rng(vec2 i) 
{
	return fract(sin(dot(i, vec2(12.9898, 78.233)))*43758.5453123);
}

float RngTime(vec2 i, float speed) 
{
	return fract(sin(dot(i*mix(0.9,1.0, sin(iTime*speed)), vec2(12.9898, 78.233)))*43758.5453123);
}

vec3 NrmAtPixel(vec2 pix)
{
	vec4 pos = vec4((pix.xy * 0.5) / (depth), pix.x, 1.0) * pix.y;
	vec3 n = normalize(cross(dFdx(pos.xyz), dFdy(pos.xyz))) * 0.5 + 0.5;
	return n;
}

vec3 Nrm()
{
	return NrmAtPixel(uv);
}

vec3 ChannelMix(vec3 Input, vec3 rmix, vec3 gmix, vec3 bmix)
{
	float r = (Input.r*rmix.r+Input.g*rmix.g+Input.b*rmix.b);
	float g = (Input.r*gmix.r+Input.g*gmix.g+Input.b*gmix.b);
	float b = (Input.r*bmix.r+Input.g*bmix.g+Input.b*bmix.b);
	return vec3(r,g,b);
}

vec3 ChannelMix(vec3 Input, vec3[3] arr)
{
	 return ChannelMix(Input, arr[0], arr[1], arr[2]);
}

vec3 Deuteranopia(vec3 c) 
{
	vec3[] arr = vec3[] ( vec3(0.43, 0.72, -0.15), vec3(0.34, 0.57, 0.09), vec3(-0.02, 0.03, 1.0) );
	return ChannelMix(c, arr);
}

vec3 Protanopia(vec3 c) 
{
	vec3[] arr = vec3[] ( vec3(0.20, 0.99, -0.19), vec3(0.16, 0.79, 0.04), vec3(0.01, -0.01, 1.0) );
	return ChannelMix(c, arr);
}

vec3 Tritanopia(vec3 c)
{
	vec3[] arr = vec3[] ( vec3(0.97, 0.11, -0.08), vec3(0.02, 0.82, 0.16), vec3(-0.06, 0.88, 0.18) );
	return ChannelMix(c, arr);
}

vec3 FakeInfrared(vec3 c)
{
	vec3 fI = vec3(-0.70, 2.0, -0.30);
	vec3[] arr = vec3[] ( fI, fI, fI );
	return 1.0 - ChannelMix(c, arr);
}

vec3 Shade() 
{
	if (depth >= far) { return Color.rgb; }
	float size = 0.001;
	float weight = 1.0;
	float strength = 0.25;
	for (float x = uv.x - size; x < uv.x + size; x += 0.01) 
	{
		for (float y = uv.y - size; y < uv.y + size; y += 0.01) 
		{
			float d = DepthAtPixel(vec2(x,y));
			if (d > depth)
			{
				weight -= strength;
			}
		}
	}
	//return Color.rgb * weight;
	return vec3(weight);
}

void main () 
{
	outColor = vec4(Color.rgb, 1.0);
	//outColor = vec4(Color.rgb, 1);
}