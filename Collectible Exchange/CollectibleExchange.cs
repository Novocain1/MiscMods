using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
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
        ICoreServerAPI sapi;
        ICoreClientAPI capi;
        const string descriptionMsg = "Allows a player to create a collectible exchange from the chest you are looking at.";
        const string syntaxMsg = "Syntax: /ce [create|update|list]";

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockEntityClass("Shop", typeof(BlockEntityShop));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            api.Event.MouseDown += Interaction;
        }

        private void Interaction(MouseEvent e)
        {
            if (e.Button == EnumMouseButton.Right && capi?.World?.Player?.CurrentBlockSelection?.BlockEntity(capi) is BlockEntityContainer) capi.SendChatMessage("/ce trade");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            api.RegisterCommand("collectibleexchange", descriptionMsg, syntaxMsg, (byPlayer, id, args)
                => CmdCollectibleExchange(byPlayer, id, args));
            api.RegisterCommand("ce", descriptionMsg, syntaxMsg, (byPlayer, id, args) 
                => CmdCollectibleExchange(byPlayer, id, args));
        }



        private void ExchangeEvent(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            (blockSel?.BlockEntity(sapi) as BlockEntityShop)?.Exchange(byPlayer);
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
                        if (!sapi.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.Use)) break;
                        BlockEntityGenericTypedContainer be = (sapi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer);
                        if (be != null)
                        {
                            List<Exchange> exchanges = GetExchanges(be.Inventory);
                            sapi.World.BlockAccessor.RemoveBlockEntity(pos);
                            sapi.World.BlockAccessor.SpawnBlockEntity("Shop", pos);
                            BlockEntityShop beShop = (sapi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityShop);
                            beShop.inventory = (InventoryGeneric)be.Inventory;
                            beShop.Exchanges = exchanges;
                            sapi.World.PlaySoundAt(AssetLocation.Create("sounds/effect/latch"), pos.X, pos.Y, pos.Z);
                        }
                    }
                    break;
                case "update":
                    if (!sapi.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.Use)) break;
                    BlockEntityShop shop = (sapi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityShop);
                    if (shop != null) shop.Exchanges = GetExchanges(shop.inventory);
                    sapi.World.PlaySoundAt(AssetLocation.Create("sounds/effect/latch"), pos.X, pos.Y, pos.Z);
                    break;
                case "list":
                    if (sapi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityShop)
                    {
                        StringBuilder builder = new StringBuilder();
                        ((BlockEntityShop)sapi.World.BlockAccessor.GetBlockEntity(pos)).GetBlockInfo(byPlayer, builder);
                        byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, builder.ToString(), EnumChatType.OwnMessage);
                    }
                    break;
                case "trade":
                    ExchangeEvent(byPlayer, byPlayer.CurrentBlockSelection);
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
                        exchange.Input = val.Itemstack.Clone();
                    }
                    else if (exchange.Output == null)
                    {
                        exchange.Output = val.Itemstack.Clone();
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
            if (Api.Side.IsServer())
            {
                foreach (var val in Exchanges)
                {
                    ItemSlot active = byPlayer.InventoryManager.ActiveHotbarSlot;
                    if (ExchangePossible(val, active, out ItemSlot exchangeSlot, out ItemSlot emptySlot))
                    {
                        if (active.TryPutInto(Api.World, emptySlot, val.Input.StackSize) == val.Input.StackSize)
                        {
                            DummySlot dummy = new DummySlot();
                            if (exchangeSlot.TryPutInto(Api.World, dummy, val.Output.StackSize) == val.Output.StackSize)
                            {
                                if (!byPlayer.InventoryManager.TryGiveItemstack(dummy.Itemstack))
                                {
                                    byPlayer.Entity.World.SpawnItemEntity(dummy.Itemstack, Pos.ToVec3d());
                                }
                                Api.World.PlaySoundAt(AssetLocation.Create("sounds/effect/cashregister"), Pos.X, Pos.Y, Pos.Z);
                                inventory.Sort(Api, EnumSortMode.ID);
                                return true;
                            }
                        }
                    }
                }
            }
            else return true;

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

            if (emptySlot != null && inventory.Any(a =>
                {
                    if (a.Itemstack?.StackSize >= exchange.Output?.StackSize && (a.Itemstack?.Collectible?.Code?.ToString() == exchange.Output?.Collectible?.Code?.ToString() && a.Itemstack?.StackSize >= exchange.Output?.StackSize))
                    {
                        _exchangeSlot = a;
                        return true;
                    }
                    return false;
                }) && exchange?.Input?.StackSize <= slot?.Itemstack?.StackSize && exchange?.Input?.Collectible?.Code == slot?.Itemstack?.Collectible?.Code) {
                    exchangeSlot = _exchangeSlot;
                    return exchangeSlot != null;
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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder builder)
        {
            builder.AppendLine().AppendLine("Can Exchange:");
            foreach (var val in Exchanges)
            {
                string ib = val.Input.Class == EnumItemClass.Block ? "block-" : "item-", ob = val.Output.Class == EnumItemClass.Block ? "block-" : "item-";
                builder.AppendLine(val.Input.StackSize + "x " + Lang.Get(ib + val.Input.Collectible.Code.ToShortString()) + " For " + val.Output.StackSize + "x " + Lang.Get(ob + val.Output.Collectible.Code.ToShortString()));
            }
        }
    }

    public class Exchange
    {
        public Exchange(ItemStack input = null, ItemStack output = null)
        {
            Input = input?.Clone();
            Output = output?.Clone();
        }

        public ItemStack Input { get; set; }
        public ItemStack Output { get; set; }
    }
}
