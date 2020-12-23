using HarmonyLib;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace RandomTests
{
    public class FallingTree : ModSystem
    {
        public const string PatchCode = "RandomTests.Modsystem.FallingTree";
        public Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            harmony = new Harmony(PatchCode);
            harmony.PatchAll();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                api.Shader.ReloadShaders();
            };
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(PatchCode);
        }
    }
}
