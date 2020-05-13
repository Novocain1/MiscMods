using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BlockAnimTest
{
    public class DisplayShaderProgram : ShaderProgramStandard, IDisplayShaderProgram
    {
        public Vec2f Resolution { set { Uniform("resolution", value); } }
        public int ScreenColors { set { BindTexture2D("screenColors", value); } }
        public float DisplayBrightness { set { Uniform("brightness", value); } }
    }
}
