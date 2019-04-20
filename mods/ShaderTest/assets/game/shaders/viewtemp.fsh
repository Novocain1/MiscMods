#version 330 core
in vec2 uv;
out vec4 outColor;

uniform sampler2D iDepthBuffer;

uniform vec2 iResolution;
uniform vec2 iMouse;
uniform vec2 iCamera;
uniform vec3 iSunPos;
uniform vec3 iMoonPos;
uniform vec3 iPlayerPosition;
uniform vec3 iLookBlockPos;
uniform vec3 iLookEntityPos;

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

float LinearizeDepth()
{
	return texture(iDepthBuffer, uv).x;
}

void main () {
	vec2 uv2 = uv - 0.5;
	uv2.x *= iResolution.x / iResolution.y;
	
	vec2 pos = vec2(-0.425,-0.492);	
	vec3 col = mix(blue, red, iTempScalar);

	float circle = Circle(uv2, pos, (0.0125*0.5), 0.001);
	for (int i = 0; i < 8; i++){
		circle += Circle(uv2, pos+vec2(0,i*0.0058), (0.01*0.5), 0.001);
	}
    outColor = vec4(col, circle);
}


