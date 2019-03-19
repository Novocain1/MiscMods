#version 330 core
#extension GL_ARB_explicit_attrib_location: enable
layout(location = 0) in vec3 vertex;
out vec2 uv;

uniform vec2 iResolution;
uniform vec2 iMouse;
uniform vec2 iCamera;
uniform vec3 iSunPos;
uniform vec3 iMoonPos;
uniform vec3 iPlayerPosition;
uniform float iMoonPhase;
uniform float iTime;
uniform float iTemperature;
uniform float iRainfall;

void main(void)
{
    gl_Position = vec4(vertex.xy, 0, 1);
    uv = (vertex.xy + 1.0) / 2.0;
}