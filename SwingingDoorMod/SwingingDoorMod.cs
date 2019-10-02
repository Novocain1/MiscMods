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

namespace TheNeolithicMod
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
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntitySwingingDoor)?.OnBlockInteract(world, byPlayer, blockSel);
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }

    class BlockEntitySwingingDoor : BlockEntity, IBlockShapeSupplier
    {
        Block OwnBlock { get => api.World.BlockAccessor.GetBlock(pos);  }
        BlockEntityAnimationUtil Util { get; set; }
        string AnimKey { get; set; }
        int AnimCount { get => Util.activeAnimationsByAnimCode.Count;  }
        bool IsClosed { get => OwnBlock.Variant["state"] == "closed"; }
        Block OpenState { get => api.World.BlockAccessor.GetBlock(OwnBlock.CodeWithVariant("state", "open")); }
        Block ClosedState { get => api.World.BlockAccessor.GetBlock(OwnBlock.CodeWithVariant("state", "closed")); }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Util = new BlockEntityAnimationUtil(api, this);
            AnimKey = OwnBlock.Attributes["animKey"].AsString("anim");

            if (api.Side.IsClient())
            {
                Util.InitializeAnimator(OwnBlock.Code.ToString(), new Vec3f(OwnBlock.Shape.rotateX, OwnBlock.Shape.rotateY, OwnBlock.Shape.rotateZ));
            }
        }

        public void OnBlockInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            RegisterDelayedCallback(dt =>
            {
                if (IsClosed)
                {
                    world.BlockAccessor.SetBlock(OpenState.BlockId, pos);
                }
                else
                {
                    world.BlockAccessor.SetBlock(ClosedState.BlockId, pos);
                }
            }, OwnBlock.Attributes["animLength"].AsInt(30) * 34);

            if (world.Side.IsClient())
            {
                AnimationMetaData data = new AnimationMetaData { Animation = AnimKey, Code = AnimKey };
                Util.StartAnimation(data);
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            Util.StopAnimation(AnimKey);
        }

        public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) => AnimCount > 0;
    }
}
