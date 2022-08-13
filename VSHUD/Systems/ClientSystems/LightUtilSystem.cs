using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace VSHUD
{
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

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => LightHighlight();

        List<BlockPos> tempPositions = new List<BlockPos>();
        List<int> tempColors = new List<int>();

        public void Reset()
        {
            tempPositions.Clear();
            tempColors.Clear();
        }

        public void AddColor(BlockPos pos, int color)
        {
            tempPositions.Add(pos);
            tempColors.Add(color);
        }

        List<BlockPos> emptyList = new List<BlockPos>();
        int tempColor = 0;
        
        BlockPos start = new BlockPos();
        BlockPos end = new BlockPos();
        BlockPos dPos = new BlockPos();
        BlockPos cPos = new BlockPos();
        BlockPos uPos = new BlockPos();
        public void LightHighlight(BlockPos pos = null)
        {
            try
            {
                if (!config.LightLevels)
                {
                    ClearLightLevelHighlights();
                    return;
                }
                Reset();

                pos = pos ?? capi.World.Player.Entity.SidedPos.AsBlockPos.UpCopy();
                int rad = config.LightRadius;

                start.Set(pos.X - rad, pos.Y - rad, pos.Z - rad);
                end.Set(pos.X + rad, pos.Y + rad, pos.Z + rad);

                capi.World.BlockAccessor.WalkBlocks(start, end, (block, iPos) =>
                {
                    if (block == null || iPos == null || block.Id == 0) return;

                    dPos.Set(pos.X - iPos.X, pos.Y - iPos.Y, pos.Z - iPos.Z);
                    
                    if (!rad.InsideRadius(dPos.X, dPos.Y, dPos.Z)) return;

                    BlockEntityFarmland blockEntityFarmland = capi.World.BlockAccessor.GetBlockEntity(iPos) as BlockEntityFarmland;
                    bool abv = blockEntityFarmland == null && config.LUShowAbove;

                    cPos.Set(iPos.X, abv ? iPos.Y + 1 : iPos.Y, iPos.Z);
                    uPos.Set(iPos.X, iPos.Y + 1, iPos.Z);

                    int level = capi.World.BlockAccessor.GetLightLevel(cPos, config.LightLevelType);

                    bool rep = !config.LUSpawning || blockEntityFarmland != null || capi.World.BlockAccessor.GetBlock(uPos).IsReplacableBy(block);
                    bool opq = !config.LUOpaque || blockEntityFarmland != null || block.AllSidesOpaque;

                    if (rep && opq)
                    {
                        float fLevel = level / 32.0f;
                        int alpha = (int)Math.Round(config.LightLevelAlpha * 255);
                        if (config.Nutrients && blockEntityFarmland != null)
                        {
                            int I = config.MXNutrients ? blockEntityFarmland.Nutrients.IndexOf(blockEntityFarmland.Nutrients.Max()) : blockEntityFarmland.Nutrients.IndexOf(blockEntityFarmland.Nutrients.Min());
                            var nuti = blockEntityFarmland.Nutrients[I];
                            int scale = (int)(nuti / 50.0f * 255.0f);
                            switch (I)
                            {
                                case 0:
                                    tempColor = ColorUtil.ToRgba(alpha, 0, 0, scale);
                                    break;
                                case 1:
                                    tempColor = ColorUtil.ToRgba(alpha, 0, scale, 0);
                                    break;
                                case 2:
                                    tempColor = ColorUtil.ToRgba(alpha, scale, 0, 0);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            tempColor = level > config.LightLevelRed ? ColorUtil.ToRgba(alpha, 0, (int)(fLevel * 255), 0) : ColorUtil.ToRgba(alpha, 0, 0, (int)(Math.Max(fLevel, 0.2) * 255));
                        }

                        AddColor(iPos.Copy(), tempColor);
                    }
                });
                if (tempColors.Count < 1)
                {
                    ClearLightLevelHighlights();
                    return;
                }

                UpdateHighlights();
            }
            catch (Exception) { }
        }

        public void UpdateHighlights()
        {
            capi.Event.EnqueueMainThreadTask(() => capi.World.HighlightBlocks(capi.World.Player, config.MinLLID, tempPositions, tempColors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary), "addLU");
        }

        public void ClearLightLevelHighlights()
        {
            capi.Event.EnqueueMainThreadTask(() => capi.World.HighlightBlocks(capi.World.Player, config.MinLLID, emptyList, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes), "removeLU");
        }
    }
}
