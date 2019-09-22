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
            api.RegisterCommand("snitch", "Creates a snitch block entity.", "", (byPlayer, id, args) =>
            {
                BlockPos pos = byPlayer.CurrentBlockSelection?.Position;
                if (api.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.Use))
                {
                    string arg = args.PopWord();
                    switch (arg)
                    {
                        case "create":
                            if (pos.BlockEntity(api) == null)
                            {
                                if (!api.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11)).Any(be => be is BlockEntitySnitch))
                                {
                                    api.World.BlockAccessor.SpawnBlockEntity("Snitch", pos);
                                    ((BlockEntitySnitch)pos.BlockEntity(byPlayer.Entity.World)).OwnerUID = byPlayer.PlayerUID;
                                }
                                else
                                {
                                    api.SendMessage(byPlayer, 0, "Already exists a snitch within 11 blocks!", EnumChatType.OwnMessage);
                                }
                            }
                            else
                            {
                                api.SendMessage(byPlayer, 0, "Cannot create a snitch where there already exists a Block Entity!", EnumChatType.OwnMessage);
                            }
                            break;
                        case "remove":
                            if (pos.BlockEntity(api.World) is BlockEntitySnitch)
                            {
                                api.World.BlockAccessor.RemoveBlockEntity(pos);
                            }
                            break;
                        case "info":
                            BlockEntitySnitch bes = (pos.BlockEntity(api.World) as BlockEntitySnitch);
                            if (bes != null && bes.OwnerUID == byPlayer.PlayerUID)
                            {
                                api.SendMessage(byPlayer, 0, "Last 5 breakins:", EnumChatType.OwnMessage);
                                for (int i = bes.Breakins.Count; bes.Breakins.Count - i < 5; i--)
                                {
                                    try { var x = bes.Breakins[i-1]; } catch { break; }
                                    api.SendMessage(byPlayer, 0, bes.Breakins[i-1], EnumChatType.OwnMessage);
                                }
                            }
                            else if (api.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11)).Any(be => be is BlockEntitySnitch && (be as BlockEntitySnitch)?.OwnerUID == byPlayer.PlayerUID))
                            {
                                foreach (var val in api.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11)))
                                {
                                    var be = (val as BlockEntitySnitch);
                                    if (val is BlockEntitySnitch && be != null && be.OwnerUID == byPlayer.PlayerUID)
                                    {
                                        api.SendMessage(byPlayer, 0, "Last 5 breakins:", EnumChatType.OwnMessage);
                                        for (int i = be.Breakins.Count; be.Breakins.Count - i < 5; i--)
                                        {
                                            try { var x = be.Breakins[i-1]; } catch { break; }
                                            api.SendMessage(byPlayer, 0, be.Breakins[i-1], EnumChatType.OwnMessage);
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                api.SendMessage(byPlayer, 0, "Must look or be in radius of a snitch, or you don't own this one!", EnumChatType.OwnMessage);
                            }
                            break;
                        default:
                            break;
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

            if (pos.BlockEntity(byPlayer.Entity.World) is BlockEntitySnitch)
            {
                byPlayer.Entity.World.BlockAccessor.RemoveBlockEntity(pos);
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

        public string OwnerUID { get; set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api.Side.IsServer())
            {
                RegisterGameTickListener(dt =>
                {
                    api.World.SpawnParticles(pos.TemporalEffectAtPos(api));
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
            OwnerUID = tree.GetString("owner");
            for (int i = 0; i < limit; i++)
            {
                string str = tree.GetString("breakins" + i);
                if (str != null) Breakins.Add(str);
            }
            base.FromTreeAtributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("owner", OwnerUID);
            for (int i = 0; i < limit; i++)
            {
                if (i >= Breakins.Count) continue;

                tree.SetString("breakins" + i, Breakins[i]);
            }
            base.ToTreeAttributes(tree);
        }
    }
}