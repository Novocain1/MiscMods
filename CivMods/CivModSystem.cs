using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CivMods
{
    class CivModSystem : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.DidPlaceBlock += PlaceBlockEvent;
            api.Event.DidBreakBlock += BreakBlockEvent;
            api.Event.DidUseBlock += UseBlockEvent;
        }

        private void UseBlockEvent(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos pos = blockSel?.Position;
            if (pos.GetBlock(byPlayer.Entity.World) is BlockBed)
            {
                (byPlayer as IServerPlayer).SetSpawnPosition(new PlayerSpawnPos(pos.X, pos.Y, pos.Z));
            }
        }

        private void BreakBlockEvent(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
        {
            ItemSlot offhand = byPlayer?.InventoryManager?.GetHotbarInventory()?[10];
            var handling = EnumHandHandling.Handled;
            BlockPos pos = blockSel.Position;

            (offhand?.Itemstack?.Item as ItemPlumbAndSquare)?.OnHeldAttackStart(offhand, byPlayer.Entity, blockSel, null, ref handling);
            if (handling != EnumHandHandling.Handled)
            {
                byPlayer.Entity.World.BlockAccessor.BreakBlock(pos, byPlayer);
            }

            List<BlockEntity> list = byPlayer.Entity.World.GetBlockEntities(pos, new Vec2i(11, 11));
            list.All(e =>
            {
                (e as BlockEntitySnitch)?.NotifyOfBreak(byPlayer, pos);
                return e is BlockEntitySnitch;
            });
        }

        private void PlaceBlockEvent(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
        {
            ItemSlot offhand = byPlayer?.InventoryManager?.GetHotbarInventory()?[10];
            var handling = EnumHandHandling.Handled;

            (offhand?.Itemstack?.Item as ItemPlumbAndSquare)?.OnHeldInteractStart(offhand, byPlayer.Entity, blockSel, null, true, ref handling);
        }
    }

    class BlockEntitySnitch : BlockEntity
    {
        public List<string> Breakins = new List<string>();
        public bool cooldown = true;

        public string OwnerUID {
            get
            {
                foreach (var val in api.World.Claims.Get(pos))
                {
                    return val.OwnedByPlayerUid;
                }
                return api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().GetReinforcment(pos).PlayerUID;
            }
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(dt =>
            {   
                List<IPlayer> intruders = new List<IPlayer>();

                if (api.World.GetPlayersAround(pos.ToVec3d(), 13, 13).All(e => {
                    if (e.PlayerUID == OwnerUID) return false;

                    intruders.Add(e);
                    return true;
                }) && cooldown)
                {
                    LimitCheck();
                    cooldown = false;
                    foreach (var val in intruders)
                    {
                        Breakins.Add(val.PlayerName + " is inside the radius of " + pos.RelativeToSpawn(api.World) + " at " + val.Entity.LocalPos);
                    }
                    RegisterDelayedCallback(dt2 => cooldown = true, 5000);
                }
            }, 30);
        }

        public void NotifyOfBreak(IServerPlayer byPlayer, BlockPos pos)
        {
            LimitCheck();
            Breakins.Add(byPlayer.PlayerName + " broke a block at " + pos.RelativeToSpawn(byPlayer.Entity.World));
        }

        public void LimitCheck()
        {
            if (Breakins.Count > 31) Breakins.RemoveAt(0);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            for (int i = 0; i < 32; i++)
            {
                string str = tree.GetString("breakins" + i);
                if (str != null) Breakins.Add(str);
            }
            base.FromTreeAtributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < 32; i++)
            {
                if (i > Breakins.Count) continue;

                tree.SetString("breakins" + i, Breakins[i]);
            }
            base.ToTreeAttributes(tree);
        }
    }
}