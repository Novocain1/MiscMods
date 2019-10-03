using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace SwingingDoor
{
    class SwingingDoorMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockSwingingDoor", typeof(BlockSwingingDoor));
            api.RegisterBlockEntityClass("SwingingDoor", typeof(BlockEntitySwingingDoor));
        }
    }

    class BlockSwingingDoor : Block
    {
        Block TopBlock { get => api.World.BlockAccessor.GetBlock(CodeWithVariant("ud", "top")); }
        Block BottomBlock { get => api.World.BlockAccessor.GetBlock(CodeWithVariant("ud", "bottom")); }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockPos pos = blockSel?.Position;
            if (pos != null && !world.BlockAccessor.GetBlock(pos.UpCopy()).IsReplacableBy(this)) return false;

            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Code.ToString() == BottomBlock.Code.ToString())
            {
                (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntitySwingingDoor)?.OnBlockInteract(world, byPlayer);
            }
            else if (Code.ToString() == TopBlock.Code.ToString())
            {
                (world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) as BlockEntitySwingingDoor)?.OnBlockInteract(world, byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            if (world.Side.IsServer())
            {
                if (Code.ToString() == BottomBlock.Code.ToString())
                {
                    world.BlockAccessor.SetBlock(TopBlock.Id, blockPos.UpCopy());
                }
            }

        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            base.OnBlockRemoved(world, pos);
            if (Code.ToString() == BottomBlock.Code.ToString())
            {
                world.BlockAccessor.SetBlock(0, pos.UpCopy());
            }
            else if (Code.ToString() == TopBlock.Code.ToString())
            {
                world.BlockAccessor.SetBlock(0, pos.DownCopy());
            }
        }
    }

    class BlockEntitySwingingDoor : BlockEntity, IBlockShapeSupplier
    {
        Block OwnBlock { get; set; }
        BlockEntityAnimationUtil Util { get; set; }
        string AnimKey { get; set; }
        int AnimCount { get => Util.activeAnimationsByAnimCode.Count;  }
        bool IsClosed { get => OwnBlock.Variant["state"] == "closed"; }
        Block OpenState { get => api.World.BlockAccessor.GetBlock(OwnBlock.CodeWithVariant("state", "open")); }
        Block ClosedState { get => api.World.BlockAccessor.GetBlock(OwnBlock.CodeWithVariant("state", "closed")); }
        Block BetweenState { get => api.World.BlockAccessor.GetBlock(OwnBlock.CodeWithVariant("state", "between")); }

        Block OpenTop { get => api.World.BlockAccessor.GetBlock(OpenState.CodeWithVariant("ud", "top")); }
        Block ClosedTop { get => api.World.BlockAccessor.GetBlock(ClosedState.CodeWithVariant("ud", "top")); }
        Block BetweenTop { get => api.World.BlockAccessor.GetBlock(BetweenState.CodeWithVariant("ud", "top")); }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            OwnBlock = api.World.BlockAccessor.GetBlock(pos);

            if (api.Side.IsClient() && OwnBlock.Id != 0)
            {
                Util = new BlockEntityAnimationUtil(api, this);
                AnimKey = OwnBlock.Attributes["animKey"].AsString("anim");
                Util.InitializeAnimator(OwnBlock.Code.ToString(), new Vec3f(OwnBlock.Shape.rotateX, OwnBlock.Shape.rotateY, OwnBlock.Shape.rotateZ));
            }
        }

        public void OnBlockInteract(IWorldAccessor world, IPlayer byPlayer)
        {
            RegisterDelayedCallback(dt =>
            {
                if (IsClosed)
                {
                    world.BlockAccessor.ExchangeBlock(OpenState.BlockId, pos);
                    world.BlockAccessor.ExchangeBlock(OpenTop.BlockId, pos.UpCopy());
                }
                else
                {
                    world.BlockAccessor.ExchangeBlock(ClosedState.BlockId, pos);
                    world.BlockAccessor.ExchangeBlock(ClosedTop.BlockId, pos.UpCopy());
                }

                world.PlaySoundAt(new AssetLocation("sounds/" + OwnBlock.Attributes["closeSound"].AsString()), byPlayer);
                Util?.StopAnimation(AnimKey);
                Initialize(api);
            }, OwnBlock.Attributes["animLength"].AsInt(30) * 31);

            if (world.Side.IsClient())
            {
                AnimationMetaData data = new AnimationMetaData { Animation = AnimKey, Code = AnimKey };
                Util.StartAnimation(data);
                world.PlaySoundAt(new AssetLocation("sounds/" + OwnBlock.Attributes["openSound"].AsString()), byPlayer);
            }

            world.BlockAccessor.ExchangeBlock(BetweenState.BlockId, pos);
            world.BlockAccessor.ExchangeBlock(BetweenTop.BlockId, pos.UpCopy());
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            Util?.StopAnimation(AnimKey);
        }

        public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) => AnimCount > 0;
    }
}
