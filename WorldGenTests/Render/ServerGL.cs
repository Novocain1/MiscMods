using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WorldGenTests
{
    public class GameWindowNative : GameWindow
    {
        public GameWindowNative(int width, int height, OpenTK.Graphics.GraphicsMode mode, GameWindowFlags flags, int openGlMajor, int openGlMinor) : base(width, height, mode, "Server Context", flags, DisplayDevice.Default, openGlMajor, openGlMinor, OpenTK.Graphics.GraphicsContextFlags.Default)
        {
        }
    }

    public class VAO
    {
        public int VaoID { get; set; }
        
        public int VaoSlotNumber { get; set; }
        public int VboIDIndex { get; set; }
        public int IndicesCount { get; set; }
        public bool Disposed { get; set; }

        public int XyzVboId { get; set; }
        public int UvVboId { get; set; }

        public void Dispose()
        {
            if (Disposed) return;

            if (XyzVboId != 0) GL.DeleteBuffer(XyzVboId);
            if (UvVboId != 0) GL.DeleteBuffer(UvVboId);

            GL.DeleteVertexArray(VaoID);

            Disposed = true;
        }
    }

    public partial class ServerGL : ModSystem
    {
        public Task Context;

        const int screenSize = 512;

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

        public static int[] pixels = new int[512 * 512];
        
        static IntPtr Address;
        static bool write = false;
        static bool read = false;

        public static void WritePtr()
        {
            while (read) ; ;

            write = true;

            GL.ReadPixels(0, 0, 512, 512, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, Address);

            write = false;
        }

        public static void ReadPtr()
        {
            while (write) ; ;
            
            read = true;

            for (int y = 0; y < 512; ++y)
            {
                for (int x = 0; x < 512; ++x)
                {
                    pixels[y * 512 + x] = Marshal.ReadInt32(Address, y * sizeof(int) * 512 + (x * sizeof(int)));
                }
            }
            
            read = false;
        }

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
                IndicesCount = 6,
                XyzVboId = xyzVboId,
                UvVboId = uvVboId
            };
        }

        VAO ScreenQuad;
        
        CancellationTokenSource tokenSource0;
        CancellationToken ct0;

        int coords;
        int scale;
        int ridgedmul;
        int sizeXY;
        int seedID;

        public override void StartServerSide(ICoreServerAPI api)
        {
            closedByUser = true;

            tokenSource0 = new CancellationTokenSource();
            ct0 = tokenSource0.Token;

            api.Event.InitWorldGenerator(() =>
            {
                CreateContext();
            }, "standard");
        }

        public void CreateContext()
        {
            Address = Marshal.AllocHGlobal(sizeof(int) * 512 * sizeof(int) * 512);

            Context = Task.Run(() =>
            {
                GraphicsMode mode = new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 32);
                GameWindowFlags flags = GameWindowFlags.Default | GameWindowFlags.UseVirtualKeys | GameWindowFlags.FixedWindow;
                OpenTK.WindowState windowstate = OpenTK.WindowState.Normal;

                GameWindowNative gamewindow = new GameWindowNative(screenSize, screenSize, mode, flags, 3, 3);
                gamewindow.VSync = VSyncMode.On;

                ScreenQuad = CreateScreenQuad();
                ProgramID = CreateShaderProgram();

                coords = GL.GetUniformLocation(ProgramID, "coords");
                scale = GL.GetUniformLocation(ProgramID, "scale");
                ridgedmul = GL.GetUniformLocation(ProgramID, "ridgedmul");
                sizeXY = GL.GetUniformLocation(ProgramID, "sizeXY");
                seedID = GL.GetUniformLocation(ProgramID, "seed");

                ErrorCode err = GL.GetError();
#if !DEBUG
                gamewindow.Visible = false;
#endif
                gamewindow.WindowState = windowstate;

                gamewindow.RenderFrame += (a, b) => OnRenderFrame(gamewindow, b, ct0);
                
                gamewindow.Closed += (a, b) =>
                {
                    ScreenQuad.Dispose();
                    if (ProgramID != 0) GL.DeleteProgram(ProgramID);
                    Marshal.FreeHGlobal(Address);
                    if (closedByUser) CreateContext();
                };

                try
                {
                    gamewindow.Run();
                }
                catch (Exception)
                {
                }

            });
        }

        static bool closedByUser = true;

        public override void Dispose()
        {
            closedByUser = false;
            tokenSource0.Cancel();
        }

        public static float xCoord, yCoord;
        public static long seed;

        private void OnRenderFrame(GameWindowNative window, FrameEventArgs args, CancellationToken ct0)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(ProgramID);
            GL.Uniform2(coords, xCoord, yCoord);
            GL.Uniform1(seedID, (float)seed % int.MaxValue);
            GL.Uniform4(scale, 1f / 128f, 1f / 512f, 1f / 256f, 1f / 720f);
            GL.Uniform1(ridgedmul, 8.0f);
            GL.Uniform1(sizeXY, screenSize);

            RenderScreenQuad();

            GL.UseProgram(0);

            window.SwapBuffers();

            WritePtr();

            if (ct0.IsCancellationRequested)
            {
                window.Close();
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