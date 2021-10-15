using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldGenTests
{
    public partial class ServerGL
    {
        const string FragCode = @"
        #version 330 core

        uniform vec2 coords;
        uniform float seed;
        uniform vec4 scale;
        uniform float ridgedmul;
        uniform int sizeXY;

        in vec2 v_texcoord;

        layout(location = 0) out vec4 outColor;
        
        vec3 mod289(vec3 x)
        {
          return x - floor(x * (1.0 / 289.0)) * 289.0;
        }

        vec4 mod289(vec4 x)
        {
          return x - floor(x * (1.0 / 289.0)) * 289.0;
        }

        vec4 permute(vec4 x)
        {
          return mod289(((x*34.0)+1.0)*x);
        }

        vec4 taylorInvSqrt(vec4 r)
        {
          return 1.79284291400159 - 0.85373472095314 * r;
        }

        vec3 fade(vec3 t) {
          return t*t*t*(t*(t*6.0-15.0)+10.0);
        }

        // Classic Perlin noise
        float cnoise(vec3 P)
        {
          vec3 Pi0 = floor(P); // Integer part for indexing
          vec3 Pi1 = Pi0 + vec3(1.0); // Integer part + 1
          Pi0 = mod289(Pi0);
          Pi1 = mod289(Pi1);
          vec3 Pf0 = fract(P); // Fractional part for interpolation
          vec3 Pf1 = Pf0 - vec3(1.0); // Fractional part - 1.0
          vec4 ix = vec4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
          vec4 iy = vec4(Pi0.yy, Pi1.yy);
          vec4 iz0 = Pi0.zzzz;
          vec4 iz1 = Pi1.zzzz;

          vec4 ixy = permute(permute(ix) + iy);
          vec4 ixy0 = permute(ixy + iz0);
          vec4 ixy1 = permute(ixy + iz1);

          vec4 gx0 = ixy0 * (1.0 / 7.0);
          vec4 gy0 = fract(floor(gx0) * (1.0 / 7.0)) - 0.5;
          gx0 = fract(gx0);
          vec4 gz0 = vec4(0.5) - abs(gx0) - abs(gy0);
          vec4 sz0 = step(gz0, vec4(0.0));
          gx0 -= sz0 * (step(0.0, gx0) - 0.5);
          gy0 -= sz0 * (step(0.0, gy0) - 0.5);

          vec4 gx1 = ixy1 * (1.0 / 7.0);
          vec4 gy1 = fract(floor(gx1) * (1.0 / 7.0)) - 0.5;
          gx1 = fract(gx1);
          vec4 gz1 = vec4(0.5) - abs(gx1) - abs(gy1);
          vec4 sz1 = step(gz1, vec4(0.0));
          gx1 -= sz1 * (step(0.0, gx1) - 0.5);
          gy1 -= sz1 * (step(0.0, gy1) - 0.5);

          vec3 g000 = vec3(gx0.x,gy0.x,gz0.x);
          vec3 g100 = vec3(gx0.y,gy0.y,gz0.y);
          vec3 g010 = vec3(gx0.z,gy0.z,gz0.z);
          vec3 g110 = vec3(gx0.w,gy0.w,gz0.w);
          vec3 g001 = vec3(gx1.x,gy1.x,gz1.x);
          vec3 g101 = vec3(gx1.y,gy1.y,gz1.y);
          vec3 g011 = vec3(gx1.z,gy1.z,gz1.z);
          vec3 g111 = vec3(gx1.w,gy1.w,gz1.w);

          vec4 norm0 = taylorInvSqrt(vec4(dot(g000, g000), dot(g010, g010), dot(g100, g100), dot(g110, g110)));
          g000 *= norm0.x;
          g010 *= norm0.y;
          g100 *= norm0.z;
          g110 *= norm0.w;
          vec4 norm1 = taylorInvSqrt(vec4(dot(g001, g001), dot(g011, g011), dot(g101, g101), dot(g111, g111)));
          g001 *= norm1.x;
          g011 *= norm1.y;
          g101 *= norm1.z;
          g111 *= norm1.w;

          float n000 = dot(g000, Pf0);
          float n100 = dot(g100, vec3(Pf1.x, Pf0.yz));
          float n010 = dot(g010, vec3(Pf0.x, Pf1.y, Pf0.z));
          float n110 = dot(g110, vec3(Pf1.xy, Pf0.z));
          float n001 = dot(g001, vec3(Pf0.xy, Pf1.z));
          float n101 = dot(g101, vec3(Pf1.x, Pf0.y, Pf1.z));
          float n011 = dot(g011, vec3(Pf0.x, Pf1.yz));
          float n111 = dot(g111, Pf1);

          vec3 fade_xyz = fade(Pf0);
          vec4 n_z = mix(vec4(n000, n100, n010, n110), vec4(n001, n101, n011, n111), fade_xyz.z);
          vec2 n_yz = mix(n_z.xy, n_z.zw, fade_xyz.y);
          float n_xyz = mix(n_yz.x, n_yz.y, fade_xyz.x); 
          return 2.2 * n_xyz;
        }

        float fnoise(vec2 p, float i)
        {
            float mat0 = 1.6;
            float mat1 = 1.2;
            float mat2 = -1.2;
            float mat3 = 1.6;

            float ox = p.x;
            float oy = p.y;
            float f = 0.0;

            f += 0.5000 * cnoise(vec3(p.x, p.y, seed + i));
            p.x = mat0 * ox + mat1 * oy;
            p.y = mat2 * ox + mat3 * oy;
            ox = p.x;
            oy = p.y;

            f += 0.2500 * cnoise(vec3(p.x, p.y, seed + i));
            p.x = mat0 * ox + mat1 * oy;
            p.y = mat2 * ox + mat3 * oy;
            ox = p.x;
            oy = p.y;

            f += 0.1250 * cnoise(vec3(p.x, p.y, seed + i));
            p.x = mat0 * ox + mat1 * oy;
            p.y = mat2 * ox + mat3 * oy;

            f += 0.0625 * cnoise(vec3(p.x, p.y, seed + i));

            return f;
        }

        void main(void)
        {
            vec2 coord = coords + v_texcoord * sizeXY;
            float nR = fnoise(coord * scale.r, 0.0) * ridgedmul;
            nR = tanh(nR * 4.0) / 2.0 + 0.5;

            nR = 1.0 - (abs(nR - 0.5) * 2.0);

            float nG = fnoise(coord * scale.g, 0.1);
            nG = tanh(nG * 4.0) / 2.0 + 0.5;

            float nB = fnoise(coord * scale.b, 0.2);
            nB = tanh(nB * 4.0) / 2.0 + 0.5;

            float nA = fnoise(coord * scale.a, 0.3);
            nA = tanh(nA * 4.0) / 2.0 + 0.5;

            outColor = vec4(nR, nG, nB, nA);
        }
        ";

        const string VertCode = @"
        #version 330 core

        out vec2 v_texcoord;

        void main(void)
        {
            // https://rauwendaal.net/2014/06/14/rendering-a-screen-covering-triangle-in-opengl/
            float x = -1.0 + float((gl_VertexID & 1) << 2);
            float y = -1.0 + float((gl_VertexID & 2) << 1);
            gl_Position = vec4(x, y, 0.0, 1.0);
            v_texcoord = vec2((x + 1.0) * 0.5, (y + 1.0) * 0.5);
        }
        ";
    }
}
