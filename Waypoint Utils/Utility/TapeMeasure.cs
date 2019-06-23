using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WaypointUtils
{
    class TapeMeasure : ModSystem
    {
        ICoreClientAPI capi;
        BlockPos start = new BlockPos(0, 0, 0);
        BlockPos end = new BlockPos(0, 0, 0);

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            capi.RegisterCommand("measure", "Tape Measure", "[start|end|calc]", new ClientChatCommandDelegate(CmdMeasuringTape));
        }

        public void CmdMeasuringTape(int groupId, CmdArgs args)
        {
            string arg = args.PopWord();
            switch (arg)
            {
                case "start":
                    if (capi.World.Player.CurrentBlockSelection != null)
                    {
                        start = capi.World.Player.CurrentBlockSelection.Position;
                        //capi.ShowChatMessage("Okay, start set to: " + start);
                        MakeHighlights();
                    }
                    else capi.ShowChatMessage("Please look at a block.");
                    break;
                case "end":
                    if (capi.World.Player.CurrentBlockSelection != null)
                    {
                        end = capi.World.Player.CurrentBlockSelection.Position;
                        //capi.ShowChatMessage("Okay, end set to: " + end);
                        MakeHighlights();
                    }
                    else capi.ShowChatMessage("Please look at a block.");
                    break;
                case "calc":
                    string type = args.PopWord();
                    switch (type)
                    {
                        case "block":
                            capi.ShowChatMessage("Block Distance: " + Math.Round(start.DistanceTo(end) + 1));
                            break;
                        case "euclidian":
                            capi.ShowChatMessage("Euclidian Distance: " + start.DistanceTo(end));
                            break;
                        case "manhattan":
                            capi.ShowChatMessage("Manhattan Distance: " + start.ManhattenDistance(end));
                            break;
                        case "horizontal":
                            capi.ShowChatMessage("Horizontal Distance: " + Math.Sqrt(start.HorDistanceSqTo(end.X, end.Z)));
                            break;
                        case "horizontalmanhattan":
                            capi.ShowChatMessage("Horizontal Manhattan Distance: " + start.HorizontalManhattenDistance(end));
                            break;
                        default:
                            capi.ShowChatMessage("Syntax: .measure calc [block|euclidian|manhattan|horizontal|horizontalmanhattan]");
                            break;
                    }
                    break;
                default:
                    capi.ShowChatMessage("Syntax: .measure [start|end|calc]");
                    break;
            }
        }

        public void MakeHighlights()
        {
            List<BlockPos> startBlock = new List<BlockPos>() { start.AddCopy(0, 1, 0), start.AddCopy(1, 0, 1) };
            List<BlockPos> endBlock = new List<BlockPos>() { end.AddCopy(0, 1, 0), end.AddCopy(1, 0, 1) };
            List<int> startcolor = new List<int>() { ColorUtil.ToRgba((int)(0.5 * 255), 0, 255, 0) };
            List<int> endcolor = new List<int>() { ColorUtil.ToRgba((int)(0.5 * 255), 0, 0, 255) };

            capi.World.HighlightBlocks(capi.World.Player, 4, startBlock, startcolor, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
            capi.World.HighlightBlocks(capi.World.Player, 5, endBlock, endcolor, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);

            capi.World.RegisterCallback(dt => ClearMeasureHighlights(), 1000);
        }

        public void ClearMeasureHighlights()
        {
            capi.World.HighlightBlocks(capi.World.Player, 4, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
            capi.World.HighlightBlocks(capi.World.Player, 5, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
        }
    }
}
