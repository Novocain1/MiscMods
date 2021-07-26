using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    public class AutoWaypoints : ClientModSystem
    {
        public class WaypointClassification
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public string Color { get; set; }
        }

        ICoreClientAPI capi;

        public Dictionary<AssetLocation, WaypointClassification> WaypointClassifications = new Dictionary<AssetLocation, WaypointClassification>();

        public AssetLocation GetClassifiedAssetLocation(AssetLocation loc, string category)
        {
            return new AssetLocation(string.Format("{0}:{1}-{2}", loc.Domain, category, loc.ToShortString()));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            
            WaypointClassifications.Clear();
            api.Event.LevelFinalize += () =>
            {
                foreach (var block in api.World.Blocks)
                {
                    if (block.Code == null) continue;
                    string col = ColorUtil.Int2Hex(~block.GetColor(api, new BlockPos()));
                    col = col.Insert(1, "FF");
                    AssetLocation loc = GetClassifiedAssetLocation(block.Code, "block");
                    WaypointClassifications[loc] = new WaypointClassification()
                    {
                        Name = Lang.GetMatching(loc.ToString()),
                        Color = col
                    };
                }

                foreach (var entity in api.World.EntityTypes)
                {
                    if (entity.Code == null) continue;

                    AssetLocation loc = GetClassifiedAssetLocation(entity.Code, "item-creature");
                    WaypointClassifications[loc] = new WaypointClassification()
                    {
                        Name = Lang.GetMatching(loc.ToString()),
                        Color = "#FFFF0000"
                    };
                }
            };

            api.Input.RegisterHotKey("vshud.markwaypoint", "(VSHUD) Mark Waypoint", GlKeys.BackSlash);
            api.Input.SetHotKeyHandler("vshud.markwaypoint", (a) =>
            {
                MarkWPForSelection(api.World.Player);
                return true;
            });
        }

        public void MarkWPForSelection(IClientPlayer player)
        {
            if (player.CurrentBlockSelection != null)
            {
                MarkWPForBlockSel(player.CurrentBlockSelection);
            }
            else if (player.CurrentEntitySelection != null)
            {
                MarkWPForEntitySel(player.CurrentEntitySelection);
            }
        }

        public void MarkWPForEntitySel(EntitySelection sel)
        {
            if (sel.Entity != null)
            {
                MarkWPForEntity(sel.Entity);
            }
        }

        public void MarkWPForBlockSel(BlockSelection sel)
        {
            if (sel.Position != null)
            {
                Block block = capi.World.BlockAccessor.GetBlock(sel.Position);
                if (block != null)
                {
                    MarkWPForBlockInPos(block, sel.Position);
                }
            }
        }

        public void MarkWPForBlockInPos(Block block, BlockPos pos)
        {
            if (FloatyWaypointManagement.WaypointElements.Any(a => a.waypointPos.AsBlockPos.Equals(pos))) return;

            var midPos = pos.ToVec3d().Add(0.5, 0.5, 0.5);
            AssetLocation loc = GetClassifiedAssetLocation(block.Code, "block");

            var wp = WaypointClassifications[loc];
            MarkWP(wp, midPos);
        }

        public void MarkWPForEntity(Entity entity)
        {
            if (FloatyWaypointManagement.WaypointElements.Any(a => a.waypointPos.AsBlockPos.Equals(entity.Pos.AsBlockPos))) return;
            AssetLocation loc = GetClassifiedAssetLocation(entity.Code, "item-creature");
            
            var wp = WaypointClassifications[loc];
            MarkWP(wp, entity.Pos.XYZ);
        }

        public void MarkWP(WaypointClassification wp, Vec3d pos)
        {
            string str = string.Format(System.Globalization.CultureInfo.InvariantCulture, "/waypoint addati {0} ={1} ={2} ={3} {4} #{5} {6}", wp.Icon ?? "star1", pos.X, pos.Y, pos.Z, false, wp.Color, wp.Name);

            capi.SendChatMessage(str);
        }
    }
}
