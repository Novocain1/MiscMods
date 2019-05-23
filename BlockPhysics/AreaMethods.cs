using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace StandAloneBlockPhysics
{
    class AreaMethods
    {
        public static BlockPos[] FullAreaAround(BlockPos pos)
        {
            BlockPos[] positions = AreaBelow(pos).Concat(AreaAbove(pos)).ToArray();
            return positions.Concat(AreaAround(pos)).ToArray();
        }

        public static BlockPos[] AreaAround(BlockPos pos)
        {
            return new BlockPos[]
            {
               pos,
               pos.NorthCopy(),
               pos.SouthCopy(),
               pos.EastCopy(),
               pos.WestCopy(),
               pos.NorthCopy().EastCopy(),
               pos.SouthCopy().WestCopy(),
               pos.EastCopy().SouthCopy(),
               pos.WestCopy().NorthCopy(),
            };
        }

        public static BlockPos[] AreaBelow(BlockPos pos)
        {
            BlockPos aPos = pos.DownCopy();
            return new BlockPos[]
            {
               aPos,
               aPos.NorthCopy(),
               aPos.SouthCopy(),
               aPos.EastCopy(),
               aPos.WestCopy(),
               aPos.NorthCopy().EastCopy(),
               aPos.SouthCopy().WestCopy(),
               aPos.EastCopy().SouthCopy(),
               aPos.WestCopy().NorthCopy(),
            };
        }

        public static List<BlockPos> AreaBelowOffsetList()
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    positions.Add(new BlockPos(x, -1, z));
                }
            }
            return positions;
        }

        public static List<BlockPos> AreaBelowCardinalOffsetList()
        {
            List<BlockPos> positions = new List<BlockPos> {
                new BlockPos(-1,-1,0),
                new BlockPos(1,-1,0),
                new BlockPos(0,-1,-1),
                new BlockPos(0,-1,1),
            };
            return positions;
        }

        public static BlockPos[] LargeAreaBelow(BlockPos pos)
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    positions.Add(pos.AddCopy(x, -1, z));
                }
            }
            return positions.ToArray();
        }

        public static List<BlockPos> LargeAreaBelowOffsetList()
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    positions.Add(new BlockPos(x, -1, z));
                }
            }
            return positions;
        }

        public static List<BlockPos> AreaAroundOffsetList()
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        positions.Add(new BlockPos(x, y, z));
                    }
                }
            }
            return positions;
        }

        public static List<BlockPos> SphericalOffsetList(int radius)
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        if ((x*x+y*y+z*z) <= (radius*radius))
                        {
                            positions.Add(new BlockPos(x, y, z));
                        }
                    }
                }
            }
            return positions;
        }

        public static List<BlockPos> CircularOffsetList(int radius)
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if ((x * x + z * z) <= (radius * radius))
                    {
                        positions.Add(new BlockPos(x, 0, z));
                    }
                }

            }
            return positions;
        }

        public static List<BlockPos> PlanarAreaAroundOffsetList()
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    positions.Add(new BlockPos(x, 0, z));
                }

            }
            return positions;
        }

        public static List<BlockPos> CardinalOffsetList()
        {
            return new List<BlockPos>
            {
                new BlockPos(0,0,1),
                new BlockPos(1,0,0),
                new BlockPos(0,0,-1),
                new BlockPos(-1,0,0),
            };
        }

        public static BlockPos[] AreaAbove(BlockPos pos)
        {
            BlockPos aPos = pos.UpCopy();
            return new BlockPos[]
            {
               aPos,
               aPos.NorthCopy(),
               aPos.SouthCopy(),
               aPos.EastCopy(),
               aPos.WestCopy(),
               aPos.NorthCopy().EastCopy(),
               aPos.SouthCopy().WestCopy(),
               aPos.EastCopy().SouthCopy(),
               aPos.WestCopy().NorthCopy(),
            };
        }

        public static BlockPos[] Cardinal(BlockPos pos)
        {
            return new BlockPos[]
            {
               pos.NorthCopy(),
               pos.SouthCopy(),
               pos.EastCopy(),
               pos.WestCopy(),
            };
        }


        public static BlockPos[] FullCardinal(BlockPos pos)
        {
            return new BlockPos[]
            {
               pos.UpCopy(),
               pos.DownCopy(),
               pos.NorthCopy(),
               pos.SouthCopy(),
               pos.EastCopy(),
               pos.WestCopy(),
            };
        }

        public static Block[] BlockCardinal(IWorldAccessor world, BlockPos pos)
        {
            List<Block> blocks = new List<Block>();
            foreach (var val in Cardinal(pos))
            {
                blocks.Add(world.BulkBlockAccessor.GetBlock(val));
            }
            return blocks.ToArray();
        }

        public static Block[] BlockFullCardinal(IWorldAccessor world, BlockPos pos)
        {
            List<Block> blocks = new List<Block>();
            foreach (var val in FullCardinal(pos))
            {
                blocks.Add(world.BulkBlockAccessor.GetBlock(val));
            }
            return blocks.ToArray();
        }

        public static int CardinalCount(IWorldAccessor world, BlockPos pos)
        {
            List<Block> blocks = new List<Block>();
            int count = 0;
            Block block = world.BulkBlockAccessor.GetBlock(pos);
            foreach (var val in BlockCardinal(world, pos))
            {
                if (!val.IsReplacableBy(block))
                {
                    count++;
                }
            }
            return count;
        }

        public static Dictionary<BlockPos, string> CardinalDict(BlockPos pos)
        {
            return new Dictionary<BlockPos, string>
            {
                {   pos.NorthCopy(), "north" },
                {   pos.SouthCopy(), "south" },
                {   pos.EastCopy(), "east" },
                {   pos.WestCopy(), "west" }
            };
        }

        public static Dictionary<string, BlockPos> DirectionDict(BlockPos pos)
        {
            return new Dictionary<string, BlockPos>
            {
                {   "north", pos.NorthCopy() },
                {   "south", pos.SouthCopy() },
                {   "east", pos.EastCopy() },
                {   "west", pos.WestCopy() }
            };
        }

        public static Dictionary<int, string> CountString()
        {
            return new Dictionary<int, string> {
                { 0, "zero" },
                { 1, "one" },
                { 2, "two" },
                { 3, "three" },
                { 4, "four" },
                { 5, "five" },
                { 6, "six" },
                { 7, "seven" },
                { 8, "eight" },
                { 9, "nine" },
            };
        }

        public static Dictionary<string, string> CornerMatches()
        {
            return new Dictionary<string, string>
            {
                {"north","east" },
                {"south","west" },
            };
        }
	}
}
