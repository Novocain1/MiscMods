#version 330 core
in vec2 uv;
out vec4 outColor;
uniform float iTime;
uniform vec2 iResolution;

void main () {
	vec2 uv2 = vec2(uv - 0.5);
	float d = length(uv2);
	vec3 col = vec3(sin(d+iTime*4), 0.0, 0.0);
    outColor = vec4(col, col.x/2);
}