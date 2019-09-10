using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Collectible_Exchange
{
    public class CollectibleExchange : ModSystem
    {
        ICoreServerAPI api;
        const string descriptionMsg = "Allows a player to create a collectible exchange from the chest you are looking at.";
        const string syntaxMsg = "Syntax: /ce [create|update|list]";

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockEntityClass("Shop", typeof(BlockEntityShop));
            api.RegisterBlockBehaviorClass("Lockable", typeof(BlockBehaviorLockableModified));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.RegisterCommand("collectibleexchange", descriptionMsg, syntaxMsg, (byPlayer, id, args)
                => CmdCollectibleExchange(byPlayer, id, args));
            api.RegisterCommand("ce", descriptionMsg, syntaxMsg, (byPlayer, id, args) 
                => CmdCollectibleExchange(byPlayer, id, args));
        }

        public void CmdCollectibleExchange(IServerPlayer byPlayer, int id, CmdArgs args)
        {
            BlockPos pos = byPlayer?.CurrentBlockSelection?.Position;
            string arg = args.PopWord();
            switch (arg)
            {
                case "create":
                    if (pos != null)
                    {
                        if (!api.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.Use)) break;
                        BlockEntityGenericTypedContainer be = (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer);
                        if (be != null)
                        {
                            List<Exchange> exchanges = GetExchanges(be.Inventory);
                            api.World.BlockAccessor.RemoveBlockEntity(pos);
                            api.World.BlockAccessor.SpawnBlockEntity("Shop", pos);
                            BlockEntityShop beShop = (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityShop);
                            beShop.inventory = (InventoryGeneric)be.Inventory;
                            beShop.Exchanges = exchanges;
                            api.World.PlaySoundAt(AssetLocation.Create("sounds/effect/latch"), pos.X, pos.Y, pos.Z);
                        }
                    }
                    break;
                case "update":
                    if (!api.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.Use)) break;
                    BlockEntityShop shop = (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityShop);
                    if (shop != null) shop.Exchanges = GetExchanges(shop.inventory);
                    api.World.PlaySoundAt(AssetLocation.Create("sounds/effect/latch"), pos.X, pos.Y, pos.Z);
                    break;
                case "list":
                    if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityShop)
                    {
                        byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityShop).GetBlockInfo(byPlayer), EnumChatType.OwnMessage);
                    }
                    break;
                default:
                    byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, syntaxMsg, EnumChatType.OwnMessage);
                    break;
            }
        }

        public List<Exchange> GetExchanges(InventoryBase inv)
        {
            List<Exchange> exchanges = new List<Exchange>();
            Exchange exchange = new Exchange();
            foreach (var val in inv)
            {
                if (val.Itemstack != null)
                {
                    if (exchange.Input == null)
                    {
                        exchange.Input = val.Itemstack;
                    }
                    else if (exchange.Output == null)
                    {
                        exchange.Output = val.Itemstack;
                        if (exchange.Output != null)
                        {
                            exchanges.Add(exchange);
                            exchange = new Exchange();
                        }
                    }
                }
            }
            return exchanges;
        }
    }

    public class BlockEntityShop : BlockEntityGenericTypedContainerModified
    {
        public override InventoryGeneric inventory { get; set; }
        public override InventoryBase Inventory => inventory;
        public List<Exchange> Exchanges { get; set; } = new List<Exchange>();

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }

        public bool Exchange(IPlayer byPlayer)
        {
            if (api.Side.IsServer())
            {
                foreach (var val in Exchanges)
                {
                    ItemSlot active = byPlayer.InventoryManager.ActiveHotbarSlot;
                    if (ExchangePossible(val, active, out ItemSlot exchangeSlot, out ItemSlot emptySlot))
                    {
                        if (active.TryPutInto(api.World, emptySlot, val.Input.StackSize) == val.Input.StackSize)
                        {
                            DummySlot dummy = new DummySlot();
                            if (exchangeSlot.TryPutInto(api.World, dummy, val.Output.StackSize) == val.Output.StackSize)
                            {
                                if (!byPlayer.InventoryManager.TryGiveItemstack(dummy.Itemstack))
                                {
                                    byPlayer.Entity.World.SpawnItemEntity(dummy.Itemstack, pos.ToVec3d());
                                }
                                api.World.PlaySoundAt(AssetLocation.Create("sounds/effect/cashregister"), pos.X, pos.Y, pos.Z);
                                inventory.Sort(api, EnumSortMode.ID);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public bool ExchangePossible(Exchange exchange, ItemSlot slot, out ItemSlot exchangeSlot, out ItemSlot emptySlot)
        {
            exchangeSlot = null; emptySlot = null;
            ItemSlot _exchangeSlot = new DummySlot();

            if (slot.Itemstack == null || exchange.Input == null || exchange.Output == null) return false;
            foreach (var val in inventory)
            {
                if (val.Empty)
                {
                    emptySlot = val;
                    break;
                }
            }

            if (inventory.Any(a => a.CanTakeFrom(slot)) &&
                inventory.Any(a =>
                {
                    if (a.Itemstack?.StackSize >= exchange.Output?.StackSize && (a.Itemstack?.Collectible?.Code?.ToString() == exchange.Output?.Collectible?.Code?.ToString() && a.Itemstack?.StackSize >= exchange.Output?.StackSize))
                    {
                        _exchangeSlot = a;
                        return true;
                    }
                    return false;
                }) && exchange?.Input?.StackSize <= slot?.Itemstack?.StackSize && exchange?.Input?.Collectible?.Code == slot?.Itemstack?.Collectible?.Code) {
                    exchangeSlot = _exchangeSlot;
                    return exchangeSlot != null && emptySlot != null;
            }
            return false;
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            List <Exchange> e = new List<Exchange>();
            int count = tree.GetInt("ExchangesCount", 0);

            for (int i = 0; i < count; i++)
            {
                string strI = "Exchanges" + i + "Input", strO = "Exchanges" + i + "Output";
                if (tree.GetItemstack(strI) != null && tree.GetItemstack(strO) != null)
                {
                    tree.GetItemstack(strI).ResolveBlockOrItem(worldForResolving); tree.GetItemstack(strO).ResolveBlockOrItem(worldForResolving);
                    e.Add(new Exchange(tree.GetItemstack(strI), tree.GetItemstack(strO)));
                }
                else break;
            }
            Exchanges = e;

            base.FromTreeAtributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("ExchangesCount", Exchanges.Count);
            for (int i = 0; i < Exchanges.Count; i++)
            {
                string strI = "Exchanges" + i + "Input", strO = "Exchanges" + i + "Output";
                tree.SetItemstack(strI, Exchanges[i].Input);
                tree.SetItemstack(strO, Exchanges[i].Output);
            }
            base.ToTreeAttributes(tree);
        }

        public override string GetBlockInfo(IPlayer forPlayer)
        {
            string v = "";
            try { v = base.GetBlockInfo(forPlayer); } catch(Exception) { }
            StringBuilder builder = new StringBuilder(v).AppendLine().AppendLine("Can Exchange:");
            foreach (var val in Exchanges)
            {
                string ib = val.Input.Class == EnumItemClass.Block ? "block-" : "item-", ob = val.Output.Class == EnumItemClass.Block ? "block-" : "item-";
                builder.AppendLine(val.Input.StackSize + "x " + Lang.Get(ib + val.Input.Collectible.Code.ToShortString()) + " For " + val.Output.StackSize + "x " + Lang.Get(ob + val.Output.Collectible.Code.ToShortString()));
            }
            return builder.ToString();
        }
    }

    public class BlockBehaviorLockableModified : BlockBehaviorLockable
    {
        public BlockBehaviorLockableModified(Block block) : base(block)
        {
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            BlockEntityShop be = (blockSel.BlockEntity(world) as BlockEntityShop);
            if (be != null && be.Exchange(byPlayer))
            {
                handling = EnumHandling.PreventSubsequent;
                return false;
            }
            
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }
    }

    public class Exchange
    {
        public Exchange(ItemStack input = null, ItemStack output = null)
        {
            Input = input;
            Output = output;
        }

        public ItemStack Input { get; set; }
        public ItemStack Output { get; set; }
    }
}
