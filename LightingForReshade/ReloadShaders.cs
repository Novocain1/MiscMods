using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace LightingForReshade
{
    public class ReloadShaders : ModSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                api.Shader.ReloadShaders();
            };
        }
    }
}
