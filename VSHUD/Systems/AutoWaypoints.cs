using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            
            WaypointClassifications.Clear();
            api.Event.LevelFinalize += () =>
            {
                Lang.Load(api.World.Logger, api.World.AssetManager, ClientSettings.Language);

                foreach (var block in api.World.Blocks)
                {
                    if (block.Code == null) continue;
                    string col = ColorUtil.Int2Hex(~block.GetColor(api, new BlockPos()));
                    col = col.Insert(1, "FF");
                    WaypointClassifications[block.Code] = new WaypointClassification()
                    {
                        Name = Lang.GetMatching(block.Code.Domain + ":block-" + block.Code.ToShortString()),
                        Color = col
                    };
                }
            };

            api.Input.RegisterHotKey("vshud.markwaypoint", "(VSHUD) Mark Waypoint", GlKeys.BackSlash);
            api.Input.SetHotKeyHandler("vshud.markwaypoint", (a) =>
            {
                MarkWPForBlockSel(api.World.Player.CurrentBlockSelection);
                return true;
            });
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
            var wp = WaypointClassifications[block.Code];
            string str = string.Format(System.Globalization.CultureInfo.InvariantCulture, "/waypoint addati {0} ={1} ={2} ={3} {4} #{5} {6}", wp.Icon ?? "star1", midPos.X, midPos.Y, midPos.Z, false, wp.Color, wp.Name);

            capi.SendChatMessage(str);
        }
    }
}
