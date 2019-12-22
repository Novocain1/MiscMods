using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
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
        private LadderPlacement ladderPlacement;
        private PillarPlacement pillarPlacement;
        private TorchPlacement torchPlacement;

        public PlacementPreviewHelper()
        {
            stairsPlacement = new StairsPlacement();
            fencePlacement = new FencePlacement();
            omniRotatablePlacement = new OmniRotatablePlacement();
            omniAttatchablePlacement = new OmniAttatchablePlacement();
            horizontalAttachablePlacement = new HorizontalAttachablePlacement();
            horizontalOrientablePlacement = new HorizontalOrientablePlacement();
            ladderPlacement = new LadderPlacement();
            pillarPlacement = new PillarPlacement();
            torchPlacement = new TorchPlacement();
        }

        public Block GetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, Block invBlock, BlockSelection blockSel)
        {
            if (invBlock is BlockStairs)
            {
                if (stairsPlacement.TryGetPlacedBlock(world, byPlayer, invBlock, blockSel, out Block block))
                {
                    return block;
                }
            }
            else if (invBlock is BlockFence)
            {
                fencePlacement.GetPlacedBlock(world, invBlock, blockSel, out Block block);
                return block;
            }
            else if (invBlock is BlockTorch)
            {
                if (torchPlacement.TryGetPlacedBlock(world, byPlayer, invBlock, blockSel, out Block block)) return block;
                else return null;
            }
            else if (invBlock.HasBehavior<BlockBehaviorPillar>())
            {
                if (pillarPlacement.TryGetPlacedBlock(world, byPlayer, invBlock, blockSel, out Block block)) return block;
                else return null;
            }
            else if (invBlock.HasBehavior<BlockBehaviorOmniAttachable>())
            {
                if (omniAttatchablePlacement.TryGetPlacedBlock(world, invBlock, blockSel, out Block block)) return block;
                else return null;
            }
            else if (invBlock.HasBehavior<BlockBehaviorHorizontalAttachable>())
            {
                if (horizontalAttachablePlacement.TryGetPlacedBlock(world, invBlock, blockSel, out Block block)) return block;
                else return null;
            }
            else if (invBlock.HasBehavior<BlockBehaviorHorizontalOrientable>())
            {
                if (horizontalOrientablePlacement.TryGetPlacedBlock(world, byPlayer, invBlock, blockSel, out Block block)) return block;
                else return null;
            }
            else if (invBlock.HasBehavior<BlockBehaviorLadder>())
            {
                if (ladderPlacement.TryGetPlacedBlock(world, byPlayer, invBlock, blockSel, out Block block)) return block;
                else return null;
            }
            else if (invBlock.BlockBehaviors.Any(b => b.ToString() == "Vintagestory.ServerMods.BlockBehaviorOmniRotatable"))
            {
                if (omniRotatablePlacement.TryGetPlacedBlock(world, byPlayer, invBlock, blockSel, out Block block)) return block;
                else return null;
            }
            return invBlock;
        }
    }

    public class StairsPlacement
    {
        bool hasDownVariant { get => !(Stairs.Attributes?["noDownVariant"].AsBool() ?? false); }
        public Block Stairs { get; set; }

        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, Block block, BlockSelection blockSel, out Block outBlock)
        {
            Stairs = block;
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
            outBlock = world.BlockAccessor.GetBlock(blockCode);
            if (outBlock == null) return false;

            return true;
        }
    }

    public class OmniRotatablePlacement
    {
        private bool rotateH = false;
        private bool rotateV = false;
        private bool rotateV4 = false;
        private string facing = "player";
        private bool rotateSides = false;

        public void UpdateProps(Block block)
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

        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, Block block, BlockSelection blockSel, out Block orientedBlock)
        {
            UpdateProps(block);
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
                blockCode = block.Code;
            }

            orientedBlock = world.BlockAccessor.GetBlock(blockCode);

            return orientedBlock != null;
        }
    }

    public class FencePlacement
    {
        public string GetOrientations(IWorldAccessor world, Block block, BlockPos pos)
        {
            string orientations =
                GetFenceCode(world, block, pos, BlockFacing.NORTH) +
                GetFenceCode(world, block, pos, BlockFacing.EAST) +
                GetFenceCode(world, block, pos, BlockFacing.SOUTH) +
                GetFenceCode(world, block, pos, BlockFacing.WEST)
            ;

            if (orientations.Length == 0) orientations = "empty";

            return orientations;
        }

        private string GetFenceCode(IWorldAccessor world, Block block, BlockPos pos, BlockFacing facing)
        {
            if (ShouldConnectAt(world, block, pos, facing)) return "" + facing.Code[0];

            return "";
        }

        public void GetPlacedBlock(IWorldAccessor world, Block block, BlockSelection blockSel, out Block outBlock)
        {
            string orientations = GetOrientations(world, block, blockSel.Position);
            outBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(orientations));
            if (block == null) outBlock = block;
        }

        public bool ShouldConnectAt(IWorldAccessor world, Block fenceblock, BlockPos ownPos, BlockFacing side)
        {
            Block block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side));

            bool attrexists = block.Attributes?["fenceConnect"][side.Code].Exists == true;
            if (attrexists)
            {
                return block.Attributes["fenceConnect"][side.Code].AsBool(true);
            }

            return
                (block.FirstCodePart() == fenceblock.FirstCodePart() || block.FirstCodePart() == fenceblock.FirstCodePart() + "gate")
                || block.SideSolid[side.GetOpposite().Index];
            ;
        }
    }

    public class OmniAttatchablePlacement
    {
        public string facingCode = "orientation";

        public void UpdateProps(Block block)
        {
            facingCode = block.GetBehavior<BlockBehaviorOmniAttachable>().properties["facingCode"].AsString("orientation");
        }

        public bool TryGetPlacedBlock(IWorldAccessor world, Block block, BlockSelection blockSel, out Block orientatedBlock)
        {
            UpdateProps(block);
            if (TryAttachTo(world, block, blockSel.Position, blockSel.Face, out Block block1))
            {
                orientatedBlock = block1;
                return true;
            }

            BlockFacing[] faces = BlockFacing.ALLFACES;
            for (int i = 0; i < faces.Length; i++)
            {
                if (TryAttachTo(world, block, blockSel.Position, faces[i], out Block block2))
                {
                    orientatedBlock = block2;
                    return true;
                }
            }
            orientatedBlock = block;
            return false;
        }

        bool TryAttachTo(IWorldAccessor world, Block block, BlockPos blockpos, BlockFacing onBlockFace, out Block orientedBlock)
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
        public bool TryGetPlacedBlock(IWorldAccessor world, Block block, BlockSelection blockSel, out Block outBlock)
        {
            // Prefer selected block face
            if (blockSel.Face.IsHorizontal)
            {
                if (TryAttachTo(world, block, blockSel, out Block orientedBlock))
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
                if (TryAttachTo(world, block, blockSel, out Block orientedBlock))
                {
                    outBlock = orientedBlock;
                    return true;
                }
            }
            outBlock = block;
            return false;
        }


        bool TryAttachTo(IWorldAccessor world, Block block, BlockSelection blockSel, out Block orientedBlock)
        {
            BlockFacing oppositeFace = blockSel.Face.GetOpposite();
            orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(oppositeFace.Code));

            return orientedBlock != null;
        }
    }

    public class HorizontalOrientablePlacement
    {
        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, Block block, BlockSelection blockSel, out Block orientedBlock)
        {
            BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
            AssetLocation blockCode = block.CodeWithParts(horVer[0].Code);
            orientedBlock = world.BlockAccessor.GetBlock(blockCode);

            return orientedBlock != null;
        }
    }

    public class LadderPlacement
    {
        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, Block block, BlockSelection blockSel, out Block orientedBlock)
        {
            BlockPos pos = blockSel.Position;

            BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
            AssetLocation blockCode = block.CodeWithParts(horVer[0].Code);

            orientedBlock = world.BlockAccessor.GetBlock(blockCode);
            // Otherwise place if we have support for it
            if (HasSupport(orientedBlock, world.BlockAccessor, pos)) return true;


            // Otherwise maybe on the other side?
            blockCode = block.CodeWithParts(blockSel.Face.GetOpposite().Code);
            orientedBlock = world.BlockAccessor.GetBlock(blockCode);
            if (orientedBlock != null && HasSupport(orientedBlock, world.BlockAccessor, pos))  return true;

            return false;
        }

        public bool HasSupportUp(Block forBlock, IBlockAccessor blockAccess, BlockPos pos)
        {
            BlockFacing ownFacing = BlockFacing.FromCode(forBlock.LastCodePart());

            BlockPos upPos = pos.UpCopy();

            return
                SideSolid(blockAccess, pos, ownFacing)
                || SideSolid(blockAccess, upPos, BlockFacing.UP)
                || (pos.Y < blockAccess.MapSizeY - 1 && blockAccess.GetBlock(upPos) == forBlock && HasSupportUp(forBlock, blockAccess, upPos))
            ;
        }


        public bool HasSupportDown(Block forBlock, IBlockAccessor blockAccess, BlockPos pos)
        {
            BlockFacing ownFacing = BlockFacing.FromCode(forBlock.LastCodePart());

            BlockPos downPos = pos.DownCopy();

            return
                SideSolid(blockAccess, pos, ownFacing)
                || SideSolid(blockAccess, downPos, BlockFacing.DOWN)
                || (pos.Y > 0 && blockAccess.GetBlock(downPos) == forBlock && HasSupportDown(forBlock, blockAccess, downPos))
            ;
        }

        public bool HasSupport(Block forBlock, IBlockAccessor blockAccess, BlockPos pos)
        {
            BlockFacing ownFacing = BlockFacing.FromCode(forBlock.LastCodePart());

            BlockPos downPos = pos.DownCopy();
            BlockPos upPos = pos.UpCopy();

            return
                SideSolid(blockAccess, pos, ownFacing)
                || SideSolid(blockAccess, downPos, BlockFacing.DOWN)
                || SideSolid(blockAccess, upPos, BlockFacing.UP)
                || (pos.Y < blockAccess.MapSizeY - 1 && blockAccess.GetBlock(upPos) == forBlock && HasSupportUp(forBlock, blockAccess, upPos))
                || (pos.Y > 0 && blockAccess.GetBlock(downPos) == forBlock && HasSupportDown(forBlock, blockAccess, downPos))
            ;
        }

        public bool SideSolid(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
        {
            return blockAccess.GetBlock(pos.X + facing.Normali.X, pos.Y, pos.Z + facing.Normali.Z).SideSolid[facing.GetOpposite().Index];
        }
    }

    public class PillarPlacement
    {
        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, Block block, BlockSelection blockSel, out Block orientedBlock)
        {
            bool invertedPlacement = block.GetBehavior<BlockBehaviorPillar>().properties["invertedPlacement"].AsBool(false);
            string rotation = null;
            switch (blockSel.Face.Axis)
            {
                case EnumAxis.X: rotation = "we"; break;
                case EnumAxis.Y: rotation = "ud"; break;
                case EnumAxis.Z: rotation = "ns"; break;
            }

            if (invertedPlacement)
            {
                BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);

                if (blockSel.Face.IsVertical)
                {
                    rotation = horVer[0].Axis == EnumAxis.X ? "we" : "ns";
                }
                else
                {
                    rotation = "ud";
                }
            }

            orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(rotation));

            return orientedBlock != null;
        }
    }

    public class TorchPlacement
    {
        public bool TryGetPlacedBlock(IWorldAccessor world, IPlayer byPlayer, Block ownBlock, BlockSelection blockSel, out Block outBlock)
        {
            outBlock = null;
            BlockPos ajdPos = blockSel.GetRecommendedPos(world.Api, ownBlock);

            if (byPlayer.Entity.Controls.Sneak)
            {
                return false;
            }

            // Prefer selected block face
            if (blockSel.Face.IsHorizontal || blockSel.Face == BlockFacing.UP)
            {
                if (TryAttachTo(world, ownBlock, ajdPos, blockSel.Face, out outBlock)) return true;
            }

            // Otherwise attach to any possible face

            BlockFacing[] faces = BlockFacing.ALLFACES;
            for (int i = 0; i < faces.Length; i++)
            {
                if (faces[i] == BlockFacing.DOWN) continue;

                if (TryAttachTo(world, ownBlock, ajdPos, faces[i], out outBlock)) return true;
            }

            return false;
        }

        bool TryAttachTo(IWorldAccessor world, Block ownBlock, BlockPos blockpos, BlockFacing onBlockFace, out Block outBlock)
        {
            outBlock = null;

            BlockFacing onFace = onBlockFace;

            BlockPos attachingBlockPos = blockpos.AddCopy(onBlockFace.GetOpposite());
            Block block = world.BlockAccessor.GetBlock(world.BlockAccessor.GetBlockId(attachingBlockPos));

            if (block.CanAttachBlockAt(world.BlockAccessor, block, attachingBlockPos, onFace))
            {
                outBlock = world.BlockAccessor.GetBlock(ownBlock.CodeWithParts(onBlockFace.Code));
            }

            return outBlock != null;
        }
    }
}
