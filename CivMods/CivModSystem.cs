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
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockEntityClass("Snitch", typeof(BlockEntitySnitch));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.DidPlaceBlock += PlaceBlockEvent;
            api.Event.DidBreakBlock += BreakBlockEvent;
            api.Event.DidUseBlock += UseBlockEvent;
            api.RegisterCommand("createsnitch", "Creates a snitch block entity.", "", (byPlayer, id, args) =>
            {
                BlockPos pos = byPlayer.CurrentBlockSelection?.Position;
                if (api.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.Use))
                {
                    if (!api.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11)).Any(be => be is BlockEntitySnitch))
                    {
                        api.World.BlockAccessor.SpawnBlockEntity("Snitch", pos);
                    }
                    else
                    {
                        api.SendMessage(byPlayer, 0, "Already exists a snitch within 11 blocks!", EnumChatType.OwnMessage);
                    }
                }
            });
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

            List<BlockEntity> list = byPlayer.Entity.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11));
            list.Any(e =>
            {
                (e as BlockEntitySnitch)?.NotifyOfBreak(byPlayer, oldblockId, pos);
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
        public int limit = 512;

        public string OwnerUID {
            get
            {
                if (api.World.Claims != null && api.World.Claims.Get(pos) != null)
                {
                    foreach (var val in api.World.Claims.Get(pos))
                    {
                        return val?.OwnedByPlayerUid;
                    }
                }

                return api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.GetReinforcment(pos)?.PlayerUID;
            }
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(dt =>
            {   
                List<IPlayer> intruders = new List<IPlayer>();

                if (cooldown && api.World.GetPlayersAround(pos.ToVec3d(), 13, 13).Any(e => {
                    if (e.PlayerUID == OwnerUID) return false;

                    intruders.Add(e);
                    return true;
                }))
                {
                    LimitCheck();
                    cooldown = false;
                    foreach (var val in intruders)
                    {
                        Breakins.Add(val.PlayerName + " is inside the radius of " + pos.RelativeToSpawn(api.World).ToVec3i() + " at " + val.Entity.LocalPos.XYZInt.ToBlockPos().RelativeToSpawn(api.World));
                    }
                    RegisterDelayedCallback(dt2 => cooldown = true, 5000);
                }
            }, 30);
        }

        public void NotifyOfBreak(IServerPlayer byPlayer, int oldblockId, BlockPos pos)
        {
            LimitCheck();
            Breakins.Add(byPlayer.PlayerName + " broke or tried to break a block at " + pos.RelativeToSpawn(byPlayer.Entity.World) + " with the name of " + api.World.GetBlock(oldblockId).Code);
        }

        public void LimitCheck()
        {
            if (Breakins.Count >= limit) Breakins.RemoveAt(0);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            for (int i = 0; i < limit; i++)
            {
                string str = tree.GetString("breakins" + i);
                if (str != null) Breakins.Add(str);
            }
            base.FromTreeAtributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < limit; i++)
            {
                if (i >= Breakins.Count) continue;

                tree.SetString("breakins" + i, Breakins[i]);
            }
            base.ToTreeAttributes(tree);
        }
    }
}