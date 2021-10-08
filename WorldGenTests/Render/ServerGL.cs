using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace WorldGenTests
{
    public class GameWindowNative : GameWindow
    {
        public GameWindowNative(OpenTK.Graphics.GraphicsMode mode, GameWindowFlags flags, int openGlMajor, int openGlMinor) : base(512, 512, mode, "Server Context", flags, DisplayDevice.Default, openGlMajor, openGlMinor, OpenTK.Graphics.GraphicsContextFlags.Default)
        {
        }
    }

    public class VAO
    {
        public int VaoID { get; set; }
        public int VaoSlotNumber { get; set; }
        public int VboIDIndex { get; set; }
        public int IndicesCount { get; set; }
    }

    public class ServerGL : ModSystem
    {
        public Task Context;

        const string FragCode = @"
        #version 330 core

        uniform vec2 coords;

        in vec2 v_texcoord;

        layout(location = 0) out vec4 outColor;
        
        float hash(float n) {
 	        return fract(cos(n*89.42)*343.42);
        }

        vec2 hash2(vec2 n) {
 	        return vec2(hash(n.x*23.62-300.0+n.y*34.35),hash(n.x*45.13+256.0+n.y*38.89)); 
        }

        float worley(vec2 c, float time) {
            float dis = 1.0;
            for(int x = -1; x <= 1; x++)
                for(int y = -1; y <= 1; y++){
                    vec2 p = floor(c)+vec2(x,y);
                    vec2 a = hash2(p) * time;
                    vec2 rnd = 0.5+sin(a)*0.5;
                    float d = length(rnd+vec2(x,y)-fract(c));
                    dis = min(dis, d);
                }
            return dis;
        }

        float worley5(vec2 c, float time) {
            float w = 0.0;
            float a = 0.5;
            for (int i = 0; i<5; i++) {
                w += worley(c, time)*a;
                c*=2.0;
                time*=2.0;
                a*=0.5;
            }
            return w;
        }

        void main(void)
        {
	        outColor = vec4(vec3(worley5(coords + v_texcoord, 100.0)), 0.0);
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

        static int[] quadVertices = {
            // Front face
            -1, -1,  0,
             1, -1,  0,
             1,  1,  0,
            -1,  1,  0
        };

        static int[] quadTextureCoords = {
            0, 0,
            1, 0,
            1, 1,
            0, 1
        };

        static int[] quadVertexIndices = {
            0, 1, 2,    0, 2, 3
        };

        public int CreateVertexShader()
        {
            int shaderID = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(shaderID, VertCode);
            GL.CompileShader(shaderID);
            GL.GetShader(shaderID, ShaderParameter.CompileStatus, out int outval);

            if (outval != 1)
            {

            }

            return shaderID;
        }

        public int CreateFragmentShader()
        {
            int shaderID = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(shaderID, FragCode);
            GL.CompileShader(shaderID);
            GL.GetShader(shaderID, ShaderParameter.CompileStatus, out int outval);

            if (outval != 1)
            {

            }

            return shaderID;
        }

        public int CreateShaderProgram()
        {
            int programId = GL.CreateProgram();
            GL.AttachShader(programId, CreateVertexShader());
            GL.AttachShader(programId, CreateFragmentShader());
            GL.LinkProgram(programId);

            GL.GetProgram(programId, GetProgramParameterName.LinkStatus, out int outval);

            if (outval != 1)
            {

            }

            return programId;
        }

        public int ProgramID;

        public VAO CreateScreenQuad()
        {
            float[] xyz = new float[12];
            float[] uv = new float[8];
            int[] indices = quadVertexIndices;

            for (int i = 0; i < 4; i++)
            {
                xyz[i * 3 + 0] = -1 + (quadVertices[i * 3 + 0] > 0 ? 2 : 0);
                xyz[i * 3 + 1] = -1 + (quadVertices[i * 3 + 1] > 0 ? 2 : 0);
                xyz[i * 3 + 2] = 0;
                uv[i * 2 + 0] = quadTextureCoords[i * 2 + 0];
                uv[i * 2 + 1] = quadTextureCoords[i * 2 + 1];
            }

            int vaoID = GL.GenVertexArray();
            int vaoSlotNumber = 0;

            GL.BindVertexArray(vaoID);
            
            int xyzVboId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, xyzVboId);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 4, xyz, BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(vaoSlotNumber, 3, VertexAttribPointerType.Float, false, 0, 0);

            vaoSlotNumber++;

            int uvVboId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboId);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 8, uv, BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(vaoSlotNumber, 2, VertexAttribPointerType.Float, false, 0, 0);

            vaoSlotNumber++;

            int vboIdIndex = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboIdIndex);
            GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(int) * 6, indices, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.BindVertexArray(0);

            return new VAO()
            {
                VaoID = vaoID,
                VaoSlotNumber = vaoSlotNumber,
                VboIDIndex = vboIdIndex,
                IndicesCount = 6
            };
        }

        VAO ScreenQuad;
        
        CancellationTokenSource tokenSource2;
        CancellationToken ct;

        public override void StartServerSide(ICoreServerAPI api)
        {
            tokenSource2 = new CancellationTokenSource();
            ct = tokenSource2.Token;

            Context = Task.Run(() =>
            {
                GraphicsMode mode = new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 24);
                GameWindowFlags flags = GameWindowFlags.Default | GameWindowFlags.UseVirtualKeys | GameWindowFlags.FixedWindow;
                OpenTK.WindowState windowstate = OpenTK.WindowState.Normal;

                GameWindowNative gamewindow = new GameWindowNative(mode, flags, 3, 3);
                gamewindow.VSync = VSyncMode.On;

                ScreenQuad = CreateScreenQuad();
                ProgramID = CreateShaderProgram();

                ErrorCode err = GL.GetError();

#if !DEBUG
                gamewindow.Visible = false;
#endif
                gamewindow.WindowState = windowstate;

                gamewindow.RenderFrame += (a, b) => OnRenderFrame(gamewindow, b, ct);
                
                try
                {
                    gamewindow.Run();
                }
                catch (Exception)
                {
                }
                
            });
        }

        public override void Dispose()
        {
            tokenSource2.Cancel();
        }

        public static float xCoord, yCoord;

        private void OnRenderFrame(GameWindowNative window, FrameEventArgs args, CancellationToken ct)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            int coords = GL.GetUniformLocation(ProgramID, "coords");
            GL.UseProgram(ProgramID);
            GL.Uniform2(coords, xCoord / 64, yCoord / 64);
            
            RenderScreenQuad();
            
            GL.UseProgram(0);
            window.SwapBuffers();
            
            if (ct.IsCancellationRequested)
            {
                window.Dispose();
            }
        }

        private void RenderScreenQuad()
        {
            GL.BindVertexArray(ScreenQuad.VaoID);

            for (int i = 0; i < ScreenQuad.VaoSlotNumber; i++)
            {
                GL.EnableVertexAttribArray(i);
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ScreenQuad.VboIDIndex);
            GL.DrawElements(BeginMode.Triangles, ScreenQuad.IndicesCount, DrawElementsType.UnsignedInt, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            for (int i = 0; i < ScreenQuad.VaoSlotNumber; i++)
            {
                GL.DisableVertexAttribArray(i);
            }

            GL.BindVertexArray(0);
        }
    }
}