using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace BlockAnimTest
{
    public class BlockAnimMod : ModSystem
    {
        ICoreClientAPI capi;
        IDisplayShaderProgram displayShaderProgram;

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockEntityClass("Display", typeof(BlockEntityDisplay));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            
            capi.Event.ReloadShader += LoadShader;
            LoadShader();
        }

        public bool LoadShader()
        {
            displayShaderProgram = new DisplayShaderProgram();
            capi.Shader.RegisterFileShaderProgram("display", displayShaderProgram);
            return displayShaderProgram.Compile();
        }

        public IDisplayShaderProgram PreparedDisplayShader(int posX, int posY, int posZ)
        {
            Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(posX, posY, posZ);
            IDisplayShaderProgram prog = displayShaderProgram;
            
            prog.Use();

            prog.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;
            prog.RgbaTint = ColorUtil.WhiteArgbVec;
            prog.RgbaAmbientIn = capi.Render.AmbientColor;
            prog.RgbaLightIn = lightrgbs;
            prog.RgbaBlockIn = ColorUtil.WhiteArgbVec;
            prog.RgbaFogIn = capi.Render.FogColor;
            prog.NormalShaded = 1;
            prog.ExtraGlow = 0;
            prog.ExtraZOffset = 0;
            prog.OverlayOpacity = 0;
            prog.ExtraGodray = 0;
            prog.FogMinIn = capi.Render.FogMin;
            prog.FogDensityIn = capi.Render.FogDensity;
            prog.DontWarpVertices = 0;
            prog.AddRenderFlags = 0;

            return prog;
        }
    }
}
