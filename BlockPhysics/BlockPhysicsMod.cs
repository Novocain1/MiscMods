using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace StandAloneBlockPhysics
{
	public class BlockPhysicsMod : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			api.RegisterBlockBehaviorClass("UnstableFalling", typeof(AlteredBlockPhysics));
		}
	}

	public class AlteredBlockPhysics : BlockBehavior
	{
		BlockPos[] offset;
		BlockPos[] cardinal;

		public AlteredBlockPhysics(Block block) : base(block)
		{
		}

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			offset = AreaMethods.AreaBelowOffsetList().ToArray();
			List<BlockPos> blocks = AreaMethods.CardinalOffsetList();
			blocks.Add(new BlockPos(0, 1, 0));
			blocks.Add(new BlockPos(0, -1, 0));
			cardinal = blocks.ToArray();
		}

		public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
		{
			TryCollapse(world, pos);
			base.OnBlockPlaced(world, pos, ref handled);
		}

		public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
		{
			TryCollapse(world, pos);
			base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
		}

		public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
		{
			world.BlockAccessor.GetBlock(pos.UpCopy()).OnNeighourBlockChange(world, pos.UpCopy(), pos);
			base.OnBlockRemoved(world, pos, ref handling);
		}

		public void TryCollapse(IWorldAccessor world, BlockPos pos)
		{
			if (world.Side.IsClient()) return;
			world.RegisterCallbackUnique((vworld, vpos, dt) =>
			{
				BlockPos dPos = pos.AddCopy(0, -1, 0);
				Block dBlock = world.BlockAccessor.GetBlock(dPos);

				if (dBlock.IsReplacableBy(block))
				{
					MoveBlock(world, pos, pos.AddCopy(0, -1, 0));
				}
				else if (dBlock.CollisionBoxes != null && dBlock.CollisionBoxes[0].Height < 1)
				{
					world.BlockAccessor.BreakBlock(pos, null);
				}
				else if (dBlock.CollisionBoxes == null || block.CollisionBoxes[0].Length >= dBlock.CollisionBoxes[0].Length)
				{
					List<BlockPos> possiblePos = new List<BlockPos>();
					for (int i = 0; i < offset.Length; i++)
					{
						BlockPos offs = pos.AddCopy(offset[i].X, offset[i].Y, offset[i].Z);
						if (offs.Y < 0 || offs.Y > world.BlockAccessor.MapSizeY) continue;

						if (world.BulkBlockAccessor.GetBlock(offs).IsReplacableBy(block))
						{
							possiblePos.Add(offs);
						}
					}
					if (possiblePos.Count() > 0)
					{
						BlockPos toPos = possiblePos[(int)Math.Round((world.Rand.NextDouble() * (possiblePos.Count - 1)))];

						MoveBlock(world, pos, toPos);
					}
				}
			}, pos, 30);
		}

		public void MoveBlock(IWorldAccessor world, BlockPos fromPos, BlockPos toPos)
		{
			if (block.EntityClass != null)
			{
				TreeAttribute attribs = new TreeAttribute();
				BlockEntity be = world.BlockAccessor.GetBlockEntity(fromPos);
				if (be != null)
				{
					be.ToTreeAttributes(attribs);
					attribs.SetInt("posx", toPos.X);
					attribs.SetInt("posy", toPos.Y);
					attribs.SetInt("posz", toPos.Z);

					world.BulkBlockAccessor.SetBlock(0, fromPos);
					world.BulkBlockAccessor.SetBlock(block.BlockId, toPos);
					world.BulkBlockAccessor.Commit();
					BlockEntity be2 = world.BlockAccessor.GetBlockEntity(toPos);

					if (be2 != null) be2.FromTreeAtributes(attribs, world);
				}
			}
			else
			{
				world.BulkBlockAccessor.SetBlock(0, fromPos);
				world.BulkBlockAccessor.SetBlock(block.BlockId, toPos);
				world.BulkBlockAccessor.Commit();
			}
		}
	}

}
