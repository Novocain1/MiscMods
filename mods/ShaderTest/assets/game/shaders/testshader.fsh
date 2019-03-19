#version 330 core
in vec2 uv;
out vec4 outColor;
uniform vec2 iResolution;
uniform vec2 iMouse;
uniform vec2 iCamera;
uniform vec3 iSunPos;
uniform vec3 iMoonPos;
uniform vec3 iPlayerPosition;
uniform float iMoonPhase;
uniform float iTime;

void main () {
	vec2 uv2 = vec2(uv - 0.5);
	float d = length(uv2);
	vec3 col = vec3(sin(d), 0.0, 0.0);
    outColor = vec4(col, col.x/2);
}