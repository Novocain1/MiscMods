using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace VSHUD
{
    class LightUtilModSystem : VSHUDClientSystem
    {
        ICoreClientAPI capi;
        VSHUDConfig config;
        ConfigLoader configLoader;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            configLoader = capi.ModLoader.GetModSystem<ConfigLoader>();
            config = configLoader.Config;
            capi.RegisterCommand("lightutil", "Light Util", "[lightlevel|type|radius|alpha|red]", new ClientChatCommandDelegate(CmdLightUtil));
            
            capi.Event.LevelFinalize += () =>
            {
                LightUtilThread thread = new LightUtilThread(capi, config);              
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

    class LightUtilThread
    {
        public object instance;
        Thread thread;

        public LightUtilThread(ICoreClientAPI capi, VSHUDConfig config)
        {
            var ts = AccessTools.GetTypesFromAssembly(Assembly.GetAssembly(typeof(ClientMain)));
            Type threadType = ts.Where((t, b) => t.Name == "ClientThread").Single();
            instance = AccessTools.CreateInstance(threadType);
            instance.SetField("game", capi.World as ClientMain);
            instance.SetField("threadName", "lightutil");
            instance.SetField("clientsystems", new ClientSystem[] { new LightUtilSystem(capi.World as ClientMain, config) });
            instance.SetField("lastFramePassedTime", new Stopwatch());
            instance.SetField("totalPassedTime", new Stopwatch());
            instance.SetField("paused", false);
            instance.SetField("sleepMs", 100);

            List<Thread> clientThreads = (capi.World as ClientMain).GetField<List<Thread>>("clientThreads");

            thread = new Thread(Process);
            thread.IsBackground = true;
            thread.Start();
            thread.Name = "lightutil";
            clientThreads.Add(thread);
        }

        public void Process()
        {
            instance.CallMethod("Process");
        }
    }

    class LightUtilSystem : ClientSystem
    {
        ICoreClientAPI capi;
        VSHUDConfig config;

        public LightUtilSystem(ClientMain game, VSHUDConfig config) : base(game)
        {
            this.config = config;
            capi = (ICoreClientAPI)game.Api;
        }

        public override string Name => "LightUtil";

        public override _WInHoPdqEmbfcVp6E75E2iH1nIe GetSystemType() => _WInHoPdqEmbfcVp6E75E2iH1nIe.Misc;

        public override void OnSeperateThreadGameTick(float dt) => LightHighlight();

        ConcurrentDictionary<BlockPos, int> colors = new ConcurrentDictionary<BlockPos, int>();
        ConcurrentQueue<BlockPos> emptyList = new ConcurrentQueue<BlockPos>();

        public void LightHighlight(BlockPos pos = null)
        {
            try 
            {
                if (!config.LightLevels) return;

                colors.Clear();

                pos = pos ?? capi.World.Player.Entity.SidedPos.AsBlockPos.UpCopy();
                int rad = config.LightRadius;

                capi.World.BlockAccessor.WalkBlocks(pos.AddCopy(-rad), pos.AddCopy(rad), (block, iPos) =>
                {
                    if (block == null || iPos == null) return;
                    BlockPos dPos = pos.SubCopy(iPos);
                    BlockEntityFarmland blockEntityFarmland = capi.World.BlockAccessor.GetBlockEntity(iPos) as BlockEntityFarmland;

                    BlockPos cPos = blockEntityFarmland == null && config.LUShowAbove ? iPos.UpCopy() : iPos;
                    int level = capi.World.BlockAccessor.GetLightLevel(cPos, config.LightLevelType);

                    bool rep = config.LUSpawning ? blockEntityFarmland != null || capi.World.BlockAccessor.GetBlock(iPos.UpCopy()).IsReplacableBy(block) : true;
                    bool opq = config.LUOpaque ? blockEntityFarmland != null || block.AllSidesOpaque : true;

                    if (block.BlockId != 0 && rep && opq && rad.InsideRadius(dPos.X, dPos.Y, dPos.Z))
                    {
                        int c = 0;

                        float fLevel = level / 32.0f;
                        int alpha = (int)Math.Round(config.LightLevelAlpha * 255);
                        if (config.Nutrients && blockEntityFarmland != null)
                        {
                            int I = config.MXNutrients ? blockEntityFarmland.Nutrients.IndexOf(blockEntityFarmland.Nutrients.Max()) : blockEntityFarmland.Nutrients.IndexOf(blockEntityFarmland.Nutrients.Min());
                            var nuti = blockEntityFarmland.Nutrients[I];
                            int scale = (int)((nuti / 50.0f) * 255.0f);
                            switch (I)
                            {
                                case 0:
                                    c = ColorUtil.ToRgba(alpha, 0, 0, scale);
                                    break;
                                case 1:
                                    c = ColorUtil.ToRgba(alpha, 0, scale, 0);
                                    break;
                                case 2:
                                    c = ColorUtil.ToRgba(alpha, scale, 0, 0);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            c = level > config.LightLevelRed ? ColorUtil.ToRgba(alpha, 0, (int)(fLevel * 255), 0) : ColorUtil.ToRgba(alpha, 0, 0, (int)(Math.Max(fLevel, 0.2) * 255));
                        }

                        colors[iPos.Copy()] = c;
                    }
                });
                if (colors.Count < 1)
                {
                    ClearLightLevelHighlights();
                    return;
                }

                capi.Event.EnqueueMainThreadTask(() => capi.World.HighlightBlocks(capi.World.Player, config.MinLLID, colors.Keys.ToList(), colors.Values.ToList(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary), "addLU");
            } 
            catch(Exception) { }
        }

        public void ClearLightLevelHighlights()
        {
            capi.Event.EnqueueMainThreadTask(() => capi.World.HighlightBlocks(capi.World.Player, config.MinLLID, emptyList.ToList(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes), "removeLU");
        }
    }
}
