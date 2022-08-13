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
    struct BlockHighlight
    {
        public bool empty;

        public int x;
        public int y;
        public int z;
        public int c;

        public BlockHighlight(bool empty)
        {
            this.empty = empty;
            this.x = 0;
            this.y = 0;
            this.z = 0;
            this.c = 0;
        }

        public BlockHighlight(int x, int y, int z, int c, bool empty)
        {
            this.empty = empty;
            this.x = x;
            this.y = y;
            this.z = z;
            this.c = c;
        }

        public BlockHighlight(BlockPos pos, int color)
        {
            x = pos.X;
            y = pos.Y;
            z = pos.Z;
            c = color;
            empty = false;
        }

        public void AsVanilla(out BlockPos pos, out int color)
        {
            pos = new BlockPos(x, y, z);
            color = c;
        }
    }

    struct BlockHighlights
    {
        public BlockHighlight[] blockHighlights;
        public int size;
        public int indexOfLast;

        public BlockHighlights(int size)
        {
            this.size = size;
            blockHighlights = new BlockHighlight[size];
            blockHighlights.Fill(new BlockHighlight(true));
            indexOfLast = 0;
        }

        public void Resize(int size)
        {
            this.size = size;
            blockHighlights = new BlockHighlight[size];
            blockHighlights.Fill(new BlockHighlight(true));
            indexOfLast = 0;
        }

        public void Add(BlockHighlight highlight)
        {
            if (indexOfLast < blockHighlights.Length)
            {
                blockHighlights[indexOfLast].x = highlight.x;
                blockHighlights[indexOfLast].y = highlight.y;
                blockHighlights[indexOfLast].z = highlight.z;
                blockHighlights[indexOfLast].c = highlight.c;
                blockHighlights[indexOfLast].empty = false;

                indexOfLast++;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < indexOfLast; i++)
            {
                blockHighlights[i].empty = true;
            }
            indexOfLast = 0;
        }

        public void AsVanilla(out List<BlockPos> positions, out List<int> colors)
        {
            positions = new List<BlockPos>();
            colors = new List<int>();

            for (int i = 0; i < indexOfLast; i++)
            {
                BlockHighlight highlight = blockHighlights[i];
                if (!highlight.empty)
                {
                    blockHighlights[i].AsVanilla(out BlockPos pos, out int col);
                    positions.Add(pos);
                    colors.Add(col);
                }
            }
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
            highlights = new BlockHighlights(config.LightVolume);
        }

        public override string Name => "LightUtil";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => LightHighlight();

        BlockHighlights highlights;

        public void Reset()
        {
            highlights.Clear();
        }

        public void AddColor(BlockPos pos, int color)
        {
            highlights.Add(new BlockHighlight(pos, color));
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
                
                if (highlights.size != config.LightVolume)
                {
                    highlights.Resize(config.LightVolume);
                }

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
                    int levelMax = capi.World.BlockAccessor.GetLightLevel(cPos, EnumLightLevelType.TimeOfDaySunLight);

                    bool rep = !config.LUSpawning || blockEntityFarmland != null || capi.World.BlockAccessor.GetBlock(uPos).IsReplacableBy(block);
                    bool opq = !config.LUOpaque || blockEntityFarmland != null || block.AllSidesOpaque;

                    if (rep && opq)
                    {
                        float fLevel = GameMath.Min(level, levelMax) / 32.0f;

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
                if (highlights.indexOfLast < 1)
                {
                    ClearLightLevelHighlights();
                    return;
                }

                UpdateHighlights();
            }
            catch (Exception) 
            { 
            }
        }

        public void UpdateHighlights()
        {
            highlights.AsVanilla(out List<BlockPos> hPos, out List<int> cols);

            capi.Event.EnqueueMainThreadTask(() =>
            {
                capi.World.HighlightBlocks(capi.World.Player, config.MinLLID, hPos, cols, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);
            }
            , "addLU");
        }

        public void ClearLightLevelHighlights()
        {
            capi.Event.EnqueueMainThreadTask(() => capi.World.HighlightBlocks(capi.World.Player, config.MinLLID, emptyList.ToList(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes), "removeLU");
        }
    }
}
