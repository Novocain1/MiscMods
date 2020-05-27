using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace VSHUD
{
    class TapeMeasure : ModSystem
    {
        ICoreClientAPI capi;
        BlockPos start = new BlockPos(0, 0, 0);
        BlockPos end = new BlockPos(0, 0, 0);
        HashSet<BlockPos> highlighted = new HashSet<BlockPos>();

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            capi.RegisterCommand("measure", "Tape Measure", "[start|end|calc]", new ClientChatCommandDelegate(CmdMeasuringTape));
            capi.RegisterCommand("shape", "Shape Ghost", "", new ClientChatCommandDelegate(CmdShape));
        }

        public void CmdShape(int groupId, CmdArgs args)
        {
            BlockPos pos = capi.World.Player.CurrentBlockSelection?.Position ?? capi.World.Player.Entity.Pos.AsBlockPos;
            string arg = args.PopWord();
            int radius = (int)args.PopInt(4);
            int thickness = (int)args.PopInt(1);
            int attach = (bool)args.PopBool(false) ? radius + 1 : 0;
            
            switch (arg)
            {
                case "sphere":
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            for (int z = -radius; z <= radius; z++)
                            {
                                BlockPos iPos = new BlockPos(pos.X + x, pos.Y + y + attach, pos.Z + z);
                                int r = x * x + y * y + z * z;

                                if (r <= (radius * radius) && r > (radius - thickness) * (radius - thickness) && iPos.Y > 0)
                                {
                                    highlighted.Add(iPos);
                                }
                            }
                        }
                    }
                    MakeHighlights(highlighted);
                    break;
                case "dome":
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            for (int z = -radius; z <= radius; z++)
                            {
                                if (y < 0) continue;
                                BlockPos iPos = new BlockPos(pos.X + x, pos.Y + y + attach, pos.Z + z);
                                int r = x * x + y * y + z * z;

                                if (r <= (radius * radius) && r > (radius - thickness) * (radius - thickness) && iPos.Y > 0)
                                {
                                    highlighted.Add(iPos);
                                }
                            }
                        }
                    }
                    MakeHighlights(highlighted);
                    break;
                case "cube":
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            for (int z = -radius; z <= radius; z++)
                            {
                                int r = thickness;
                                if (x < r && y < r && z < r && x > -r && y > -r && z > -r) continue;
                                BlockPos iPos = new BlockPos(pos.X + x, pos.Y + y + attach - 1, pos.Z + z);
                                if (iPos.Y > 0)
                                {
                                    highlighted.Add(iPos);
                                }
                            }
                        }
                    }
                    MakeHighlights(highlighted);
                    break;
                case "path":
                    WaypointUtilSystem wUtil = capi.ModLoader.GetModSystem<WaypointUtilSystem>();
                    if (radius > wUtil.Waypoints.Count || thickness > wUtil.Waypoints.Count) break;
                    BlockPos wp1Pos = wUtil.Waypoints[radius]?.Position?.AsBlockPos, wp2Pos = wUtil.Waypoints[thickness]?.Position?.AsBlockPos;
                    if (wp1Pos != null && wp2Pos != null)
                    {
                        highlighted = new HashSet<BlockPos>(highlighted.Concat(PlotLine3d(wp1Pos, wp2Pos)));
                    }
                    MakeHighlights(highlighted);
                    break;
                case "circle":
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int z = -radius; z <= radius; z++)
                        {
                            BlockPos iPos = new BlockPos(pos.X + x, pos.Y, pos.Z + z);
                            int r = x * x + z * z;
                            if (r <= (radius * radius) && r > (radius - thickness) * (radius - thickness) && iPos.Y > 0)
                            {
                                highlighted.Add(iPos);
                            }
                        }
                    }
                    MakeHighlights(highlighted);
                    break;
                case "extrude":
                    var tmp = new HashSet<BlockPos>();
                    var dir = thickness == 0 ? new Vec3i(0, 1, 0) : thickness == 1 ? new Vec3i(1, 0, 0) : thickness == 2 ? new Vec3i(0, 0, 1) : thickness == 3 ? new Vec3i(1, 1, 1) : new Vec3i(0, 0, 0);
                    foreach (var val in highlighted)
                    {
                        for (int i = 0; i < radius; i++)
                        {
                            tmp.Add(val.AddCopy(dir * i));
                        }
                    }
                    highlighted = new HashSet<BlockPos>(highlighted.Concat(tmp).ToList());
                    MakeHighlights(highlighted);
                    break;
                case "toflatten":
                    HashSet<BlockPos> temp = new HashSet<BlockPos>(highlighted);
                    foreach (var val in highlighted)
                    {
                        if (capi.World.BlockAccessor.GetBlock(val).Id == 0)
                        {
                            temp.Remove(val);
                        }
                    }
                    highlighted = temp;
                    MakeHighlights(highlighted);
                    break;
                case "save":
                    using (TextWriter tw = new StreamWriter("shape" + radius + ".json"))
                    {
                        tw.Write(JsonConvert.SerializeObject(highlighted, Formatting.None));
                        tw.Close();
                    }
                    break;
                case "load":
                    using (TextReader tr = new StreamReader("shape" + radius + ".json"))
                    {
                        highlighted = JsonConvert.DeserializeObject<HashSet<BlockPos>>(tr.ReadToEnd());
                        tr.Close();
                    }
                    MakeHighlights(highlighted);
                    break;
                case "clear":
                    capi.World.HighlightBlocks(capi.World.Player, 514, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
                    highlighted.Clear();
                    break;
                default:
                    break;
            }
        }

        public void CmdMeasuringTape(int groupId, CmdArgs args)
        {
            WaypointUtilSystem wUtil = capi.ModLoader.GetModSystem<WaypointUtilSystem>();
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
                case "startwp":
                    int? swpID = args.PopInt();
                    if (swpID != null)
                    {
                        start = wUtil.Waypoints[(int)swpID].Position.AsBlockPos;
                        MakeHighlights();
                    }
                    else capi.ShowChatMessage("Please enter a waypoint id.");
                    break;
                case "endwp":
                    int? ewpID = args.PopInt();
                    if (ewpID != null)
                    {
                        end = wUtil.Waypoints[(int)ewpID].Position.AsBlockPos;
                        MakeHighlights();
                    }
                    else capi.ShowChatMessage("Please enter a waypoint id.");
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

        public void MakeHighlights(HashSet<BlockPos> highlighted)
        {
            List<int> color = new List<int>() { ColorUtil.ToRgba((int)(0.25 * 255), (int)(0.25 * 255), (int)(0.5 * 255), (int)(0.5 * 255)) };
            capi.World.HighlightBlocks(capi.World.Player, 514, highlighted.ToList(), color, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);
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

        public List<BlockPos> PlotLine3d(BlockPos a, BlockPos b) => PlotLine3d(a.X, a.Y, a.Z, b.X, b.Y, b.Z);

        // http://members.chello.at/~easyfilter/bresenham.html
        public List<BlockPos> PlotLine3d(int x0, int y0, int z0, int x1, int y1, int z1)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int dz = Math.Abs(z1 - z0), sz = z0 < z1 ? 1 : -1;
            int dm = GameMath.Max(dx, dy, dz), i = dm; /* maximum difference */
            x1 = y1 = z1 = dm / 2; /* error offset */

            BlockPos pos = new BlockPos();
            HashSet<BlockPos> blocks = new HashSet<BlockPos>();
            for (;;)
            {  /* loop */
                pos.Set(x0, y0, z0);
                blocks.Add(pos.Copy());
                if (i-- == 0) break;
                x1 -= dx; if (x1 < 0) { x1 += dm; x0 += sx; }
                y1 -= dy; if (y1 < 0) { y1 += dm; y0 += sy; }
                z1 -= dz; if (z1 < 0) { z1 += dm; z0 += sz; }
            }
            return blocks.ToList();
        }
    }
}
