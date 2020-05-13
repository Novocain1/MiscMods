using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace BlockAnimTest
{
    public interface IDisplayShaderProgram : IStandardShaderProgram
    {
        Vec2f Resolution { set; }
        int ScreenColors { set; }
        float DisplayBrightness { set; }
    }
}
