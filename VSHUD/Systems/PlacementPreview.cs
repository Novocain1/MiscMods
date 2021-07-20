using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.ServerMods;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent.Mechanics;

namespace VSHUD
{
    class PlacementPreview : ClientModSystem
    {
        PlacementRenderer renderer;
        public override void StartClientSide(ICoreClientAPI api)
        {
            renderer = new PlacementRenderer(api);
            api.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);
            api.Input.RegisterHotKey("placementpreviewtoggle", "Toggle Placement Preview", GlKeys.Quote);
            api.Input.SetHotKeyHandler("placementpreviewtoggle", (a) => 
            {
                VSHUDConfig config = ConfigLoader.Config;
                config.PRShow = !config.PRShow;
                ConfigLoader.SaveConfig(api);
                return true;
            });

            api.Event.LevelFinalize += () => api.Shader.ReloadShaders();
        }
    }
}
