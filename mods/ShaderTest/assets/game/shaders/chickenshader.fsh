#version 330 core
in vec2 uv;
out vec4 outColor;

uniform vec2 iResolution;
uniform vec2 iMouse;
uniform vec2 iCamera;
uniform vec3 iSunPos;
uniform vec3 iMoonPos;
uniform vec3 iPlayerPosition;

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

//By ID, if it's null, it'll be -1
uniform float iActiveItem;
uniform float iLookingAtBlock;
uniform float iLookingAtEntity;

void main () {
	if (iActiveItem == 614) {
		vec3 col = 0.5 + 0.5*cos(iTime+uv.xyx+vec3(0,2,4));
    	outColor = vec4(col, 0.5);
	}
}