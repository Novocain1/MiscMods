using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSHUD
{
    public class PlacementPreviewHelper
    {
        private StairsPlacement stairsPlacement;
        private FencePlacement fencePlacement;
        private OmniRotatablePlacement omniRotatablePlacement;
        private OmniAttatchablePlacement omniAttatchablePlacement;
        private HorizontalAttachablePlacement horizontalAttachablePlacement;
        private HorizontalOrientablePlacement horizontalOrientablePlacement;

        public PlacementPreviewHelper()
        {
            stairsPlacement = new StairsPlacement();
            fencePlacement = new FencePlacement();
            omniRotatablePlacement = new OmniRotatablePlacement();
            omniAttatchablePlacement = new OmniAttatchablePlacement();
            horizontalAttachablePlacement = new HorizontalAttachablePlacement();
            horizontalOrientablePlacement = new HorizontalOrientablePlacement();
        }

        public Block GetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, Block invBlock, BlockSelection blockSel)
        {
            if (invBlock is BlockStairs)
            {
                stairsPlacement.Stairs = (BlockStairs)invBlock;
                if (stairsPlacement.TryGetPlacedBlock(world, byPlayer, blockSel, out Block block))
                {
                    return block;
                }
            }
            else if (invBlock is BlockFence)
            {
                fencePlacement.Fence = (BlockFence)invBlock;
                fencePlacement.GetPlacedBlock(world, byPlayer, blockSel, out Block block);
                return block;
            }
            else if (invBlock.HasBehavior<BlockBehaviorOmniAttachable>())
            {
                omniAttatchablePlacement.block = invBlock;
                omniAttatchablePlacement.UpdateProps();
                if (omniAttatchablePlacement.TryGetPlacedBlock(world, blockSel, out Block block))
                {
                    return block;
                }
            }
            else if (invBlock.HasBehavior<BlockBehaviorHorizontalAttachable>())
            {
                horizontalAttachablePlacement.block = invBlock;
                if (horizontalAttachablePlacement.TryGetPlacedBlock(world, blockSel, out Block block))
                {
                    return block;
                }
            }
            else if (invBlock.HasBehavior<BlockBehaviorHorizontalOrientable>())
            {
                horizontalOrientablePlacement.block = invBlock;
                if (horizontalOrientablePlacement.TryGetPlacedBlock(world, byPlayer, blockSel, out Block block))
                {
                    return block;
                }
            }
            else if (invBlock.BlockBehaviors.Any(b => b.ToString() == "Vintagestory.ServerMods.BlockBehaviorOmniRotatable"))
            {
                omniRotatablePlacement.block = invBlock;
                omniRotatablePlacement.UpdateProps();
                if (omniRotatablePlacement.TryGetPlacedBlock(world, byPlayer, blockSel, out Block block))
                {
                    return block;
                }
            }
            return invBlock;
        }
    }

    public class StairsPlacement
    {
        bool hasDownVariant { get => !(Stairs.Attributes?["noDownVariant"].AsBool() ?? false); }
        public BlockStairs Stairs { get; set; }

        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, out Block block)
        {
            BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);

            if (blockSel.Face.IsVertical)
            {
                horVer[1] = blockSel.Face;
            }
            else
            {
                horVer[1] = blockSel.HitPosition.Y < 0.5 || !hasDownVariant ? BlockFacing.UP : BlockFacing.DOWN;
            }

            AssetLocation blockCode = Stairs.CodeWithParts(horVer[1].Code, horVer[0].Code);
            block = world.BlockAccessor.GetBlock(blockCode);
            if (block == null) return false;

            return true;
        }
    }

    public class OmniRotatablePlacement
    {
        public Block block { get; set; }
        private bool rotateH = false;
        private bool rotateV = false;
        private bool rotateV4 = false;
        private string facing = "player";
        private bool rotateSides = false;

        public void UpdateProps()
        {
            foreach (var bh in block.BlockBehaviors)
            {
                if (bh.ToString() == "Vintagestory.ServerMods.BlockBehaviorOmniRotatable")
                {
                    rotateH = bh.properties["rotateH"].AsBool(rotateH);
                    rotateV = bh.properties["rotateV"].AsBool(rotateV);
                    rotateV4 = bh.properties["rotateV4"].AsBool(rotateV4);
                    rotateSides = bh.properties["rotateSides"].AsBool(rotateSides);
                    facing = bh.properties["facing"].AsString(facing);
                    return;
                }
            }
        }

        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, out Block orientedBlock)
        {
            AssetLocation blockCode = null;
            if (rotateSides)
            {
                // Simple 6 state rotator.

                if (facing == "block")
                {
                    var x = Math.Abs(blockSel.HitPosition.X - 0.5);
                    var y = Math.Abs(blockSel.HitPosition.Y - 0.5);
                    var z = Math.Abs(blockSel.HitPosition.Z - 0.5);
                    switch (blockSel.Face.Axis)
                    {
                        case EnumAxis.X:
                            if (z < 0.3 && y < 0.3)
                            {
                                blockCode = block.CodeWithParts(blockSel.Face.GetOpposite().Code);
                            }
                            else if (z > y)
                            {
                                blockCode = block.CodeWithParts(blockSel.HitPosition.Z < 0.5 ? "north" : "south");
                            }
                            else
                            {
                                blockCode = block.CodeWithParts(blockSel.HitPosition.Y < 0.5 ? "down" : "up");
                            }
                            break;

                        case EnumAxis.Y:
                            if (z < 0.3 && x < 0.3)
                            {
                                blockCode = block.CodeWithParts(blockSel.Face.GetOpposite().Code);
                            }
                            else if (z > x)
                            {
                                blockCode = block.CodeWithParts(blockSel.HitPosition.Z < 0.5 ? "north" : "south");
                            }
                            else
                            {
                                blockCode = block.CodeWithParts(blockSel.HitPosition.X < 0.5 ? "west" : "east");
                            }
                            break;

                        case EnumAxis.Z:
                            if (x < 0.3 && y < 0.3)
                            {
                                blockCode = block.CodeWithParts(blockSel.Face.GetOpposite().Code);
                            }
                            else if (x > y)
                            {
                                blockCode = block.CodeWithParts(blockSel.HitPosition.X < 0.5 ? "west" : "east");
                            }
                            else
                            {
                                blockCode = block.CodeWithParts(blockSel.HitPosition.Y < 0.5 ? "down" : "up");
                            }
                            break;
                    }
                }
                else
                {
                    if (blockSel.Face.IsVertical)
                    {
                        blockCode = block.CodeWithParts(blockSel.Face.GetOpposite().Code);
                    }
                    else
                    {
                        blockCode = block.CodeWithParts(BlockFacing.HorizontalFromAngle(byPlayer.Entity.Pos.Yaw).Code);
                    }
                }
            }
            else if (rotateH || rotateV)
            {
                // Complex 4/8/16 state rotator.
                string h = "north";
                string v = "up";
                if (blockSel.Face.IsVertical)
                {
                    v = blockSel.Face.Code;
                    h = BlockFacing.HorizontalFromAngle(byPlayer.Entity.Pos.Yaw).Code;
                }
                else if (rotateV4)
                {
                    if (facing == "block")
                    {
                        h = blockSel.Face.GetOpposite().Code;
                    }
                    else
                    {
                        // Default to player facing.
                        h = BlockFacing.HorizontalFromAngle(byPlayer.Entity.Pos.Yaw).Code;
                    }
                    switch (blockSel.Face.Axis)
                    {
                        case EnumAxis.X:
                            // Find the axis farther from the center.
                            if (Math.Abs(blockSel.HitPosition.Z - 0.5) > Math.Abs(blockSel.HitPosition.Y - 0.5))
                            {
                                v = blockSel.HitPosition.Z < 0.5 ? "left" : "right";
                            }
                            else
                            {
                                v = blockSel.HitPosition.Y < 0.5 ? "up" : "down";
                            }
                            break;

                        case EnumAxis.Z:
                            if (Math.Abs(blockSel.HitPosition.X - 0.5) > Math.Abs(blockSel.HitPosition.Y - 0.5))
                            {
                                v = blockSel.HitPosition.X < 0.5 ? "left" : "right";
                            }
                            else
                            {
                                v = blockSel.HitPosition.Y < 0.5 ? "up" : "down";
                            }
                            break;
                    }
                }
                else
                {
                    v = blockSel.HitPosition.Y < 0.5 ? "up" : "down";
                }

                if (rotateH && rotateV)
                {
                    blockCode = block.CodeWithParts(v, h);
                }
                else if (rotateH)
                {
                    blockCode = block.CodeWithParts(h);
                }
                else if (rotateV)
                {
                    blockCode = block.CodeWithParts(v);
                }
            }

            if (blockCode == null)
            {
                blockCode = this.block.Code;
            }

            orientedBlock = world.BlockAccessor.GetBlock(blockCode);

            return orientedBlock != null;
        }
    }

    public class FencePlacement
    {
        public BlockFence Fence { get; set; }

        public string GetOrientations(IWorldAccessor world, BlockPos pos)
        {
            string orientations =
                GetFenceCode(world, pos, BlockFacing.NORTH) +
                GetFenceCode(world, pos, BlockFacing.EAST) +
                GetFenceCode(world, pos, BlockFacing.SOUTH) +
                GetFenceCode(world, pos, BlockFacing.WEST)
            ;

            if (orientations.Length == 0) orientations = "empty";

            return orientations;
        }

        private string GetFenceCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
        {
            if (ShouldConnectAt(world, pos, facing)) return "" + facing.Code[0];

            return "";
        }

        public void GetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, out Block block)
        {
            string orientations = GetOrientations(world, blockSel.Position);
            block = world.BlockAccessor.GetBlock(Fence.CodeWithParts(orientations));
            if (block == null) block = Fence;
        }

        public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
        {
            Block block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side));

            bool attrexists = block.Attributes?["fenceConnect"][side.Code].Exists == true;
            if (attrexists)
            {
                return block.Attributes["fenceConnect"][side.Code].AsBool(true);
            }

            return
                (block.FirstCodePart() == Fence.FirstCodePart() || block.FirstCodePart() == Fence.FirstCodePart() + "gate")
                || block.SideSolid[side.GetOpposite().Index];
            ;
        }
    }

    public class OmniAttatchablePlacement
    {
        public string facingCode = "orientation";
        public Block block { get; set; }

        public void UpdateProps()
        {
            facingCode = block.GetBehavior<BlockBehaviorOmniAttachable>().properties["facingCode"].AsString("orientation");
        }

        public bool TryGetPlacedBlock(IWorldAccessor world, BlockSelection blockSel, out Block orientatedBlock)
        {
            if (TryAttachTo(world, blockSel.Position, blockSel.Face, out Block block1))
            {
                orientatedBlock = block1;
                return true;
            }

            BlockFacing[] faces = BlockFacing.ALLFACES;
            for (int i = 0; i < faces.Length; i++)
            {
                if (TryAttachTo(world, blockSel.Position, faces[i], out Block block2))
                {
                    orientatedBlock = block2;
                    return true;
                }
            }
            orientatedBlock = block;
            return false;
        }

        bool TryAttachTo(IWorldAccessor world, BlockPos blockpos, BlockFacing onBlockFace, out Block orientedBlock)
        {
            orientedBlock = null;
            BlockPos attachingBlockPos = blockpos.AddCopy(onBlockFace.GetOpposite());
            Block attachingBlock = world.BlockAccessor.GetBlock(world.BlockAccessor.GetBlockId(attachingBlockPos));

            BlockFacing onFace = onBlockFace;

            if (attachingBlock.CanAttachBlockAt(world.BlockAccessor, block, attachingBlockPos, onFace))
            {
                orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithVariant(facingCode, onBlockFace.Code));
            }

            return orientedBlock != null;
        }
    }

    public class HorizontalAttachablePlacement
    {
        public Block block { get; set; }

        public bool TryGetPlacedBlock(IWorldAccessor world, BlockSelection blockSel, out Block outBlock)
        {
            // Prefer selected block face
            if (blockSel.Face.IsHorizontal)
            {
                if (TryAttachTo(world, blockSel, out Block orientedBlock))
                {
                    outBlock = orientedBlock;
                    return true;
                }
            }

            // Otherwise attach to any possible face
            BlockFacing[] faces = BlockFacing.HORIZONTALS;
            blockSel = blockSel.Clone();
            for (int i = 0; i < faces.Length; i++)
            {
                blockSel.Face = faces[i];
                if (TryAttachTo(world, blockSel, out Block orientedBlock))
                {
                    outBlock = orientedBlock;
                    return true;
                }
            }
            outBlock = block;
            return false;
        }


        bool TryAttachTo(IWorldAccessor world, BlockSelection blockSel, out Block orientedBlock)
        {
            BlockFacing oppositeFace = blockSel.Face.GetOpposite();
            orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(oppositeFace.Code));

            return orientedBlock != null;
        }
    }

    public class HorizontalOrientablePlacement
    {
        public Block block { get; set; }

        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, out Block orientedBlock)
        {
            BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
            AssetLocation blockCode = block.CodeWithParts(horVer[0].Code);
            orientedBlock = world.BlockAccessor.GetBlock(blockCode);

            return orientedBlock != null;
        }
    }
}
