#version 330 core
#extension GL_ARB_explicit_attrib_location: enable
layout(location = 0) in vec3 vertex;
out vec2 uv;

uniform sampler2D iDepthBuffer;
uniform sampler2D iGodrays;

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
float rng = fract(sin(dot(vec2(uv.x, uv.y), vec2(12.9898 + iTime, 78.233 + iTime)))*43758.5453123 + iTime);

//Current Enum Tool Type
//NoTool: -1, Knife: 0, Pickaxe: 1, Axe: 2, Sword: 3, Shovel: 4, Hammer: 5, Spear: 6, Bow: 7, Shears: 8, Sickle: 9, Hoe: 10, Saw: 11, Chisel: 12,
uniform float iActiveTool;

void main(void)
{
    gl_Position = vec4(vertex.xy, 1, 1);
    uv = (vertex.xy + 1.0) / 2.0;
}