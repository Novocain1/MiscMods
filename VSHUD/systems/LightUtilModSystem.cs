using System;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    class LightUtilModSystem : ClientModSystem
    {
        ICoreClientAPI capi;
        VSHUDConfig config;
        ConfigLoader configLoader;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            configLoader = capi.ModLoader.GetModSystem<ConfigLoader>();
            config = ConfigLoader.Config;
            capi.RegisterCommand("lightutil", "Light Util", "[lightlevel|type|radius|alpha|red]", new ClientChatCommandDelegate(CmdLightUtil));
            
            capi.Event.LevelFinalize += () =>
            {
                capi.InjectClientThread("LightUtil", 100, new LightUtilSystem(api.World as ClientMain, config));
            };
        }

        public void CmdLightUtil(int groupId, CmdArgs args)
        {
            string arg = args.PopWord();
            switch (arg)
            {
                case "lightlevel":
                    bool? cnd = args.PopBool();
                    if (cnd != null) config.LightLevels = (bool)cnd;
                    else { config.LightLevels = !config.LightLevels; }
                    break;
                case "type":
                    int? type = args.PopInt();
                    if (type != null)
                    {
                        config.LightLevelType = (EnumLightLevelType)type;
                    }
                    capi.ShowChatMessage("Light Util Type Set To " + Enum.GetName(typeof(EnumLightLevelType), config.LightLevelType));
                    break;
                case "radius":
                    int? rad = args.PopInt();
                    if (rad != null)
                    {
                        config.LightRadius = (int)rad;
                    }
                    capi.ShowChatMessage("Light Util Radius Set To " + config.LightRadius);
                    break;
                case "alpha":
                    float? alpha = args.PopFloat();
                    config.LightLevelAlpha = alpha != null ? (float)alpha : config.LightLevelAlpha;
                    capi.ShowChatMessage("Light Util Opacity Set To " + config.LightLevelAlpha);
                    break;
                case "red":
                    int? red = args.PopInt();
                    if (red != null)
                    {
                        config.LightLevelRed = (int)red;
                    }
                    capi.ShowChatMessage("Red Level Set To " + config.LightLevelRed);
                    break;
                case "above":
                    bool? ab = args.PopBool();
                    if (ab != null) config.LightLevels = (bool)ab;
                    else { config.LUShowAbove = !config.LUShowAbove; }
                    capi.ShowChatMessage("Show Above Set To " + config.LUShowAbove);
                    break;
                case "nutrients":
                    bool? ac = args.PopBool();
                    config.Nutrients = ac ?? !config.Nutrients;
                    capi.ShowChatMessage("Show Farmland Nutrient Set To " + config.Nutrients);
                    break;
                case "mxnutrients":
                    bool? ad = args.PopBool();
                    config.MXNutrients = ad ?? !config.MXNutrients;
                    capi.ShowChatMessage(string.Format("Farmland Nutrient Display Set To {0}.", config.MXNutrients ? "Max" : "Min"));
                    break;
                default:
                    capi.ShowChatMessage("Syntax: .lightutil [lightlevel|type|radius|alpha|red|above|nutrients(enable farmland nutrient display)|mxnutrients(toggle whether to show the min or max nutrient)]");
                    break;
            }
            configLoader.SaveConfig();
        }
    }

}
