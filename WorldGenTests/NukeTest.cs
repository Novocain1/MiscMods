using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WorldGenTests
{
    /*
    public class NukeTest : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.RegisterCommand("nuke", "", "", (a, b, c) =>
            {
                if (!a.HasPrivilege("controlserver")) return;

                var bA = api.World.BulkBlockAccessor;
                int sploded = 1;

                int radius = 32;
                long id = 0;
                int splode = 0;

                BlockPos startPos = a.Entity.ServerPos.AsBlockPos;
                id = api.Event.RegisterGameTickListener((dt) =>
                {
                    if (splode < radius)
                    {
                        int rad = splode;

                        int diameter = rad * 2;

                        int probecount = (diameter * diameter * diameter) - ((diameter - 1) * (diameter - 1) * (diameter - 1));

                        int[] probes = new int[probecount];
                        int i = 0;

                        for (int x = -rad; x < rad; x++)
                        {
                            for (int y = -rad; y < rad; y++)
                            {
                                for (int z = -rad; z < rad; z++)
                                {
                                    if (i * 3 + 2 > probecount) break;

                                    if (api.World.Rand.NextDouble() > 0.9999 && InsideRadius(rad, x, y, z) && !InsideRadius(rad - 1, x, y, z))
                                    {
                                        //bA.SetBlock(710, startPos.AddCopy(x, y, z));
                                        probes[i * 3 + 0] = startPos.X + x;
                                        probes[i * 3 + 1] = startPos.Y + y;
                                        probes[i * 3 + 2] = startPos.Z + z;
                                        i++;
                                    }
                                }
                            }
                        }

                        Vec3d fromPos = new Vec3d(startPos.X, startPos.Y, startPos.Z);

                        Vec3d toPos = new Vec3d();

                        List<BlockSelection> blockIntercepts = new List<BlockSelection>();
                        List<EntitySelection> entityIntercepts = new List<EntitySelection>();

                        for (int j = 0; j < i / 3; j++)
                        {
                            toPos.X = probes[j * 3 + 0];
                            toPos.Y = probes[j * 3 + 1];
                            toPos.Z = probes[j * 3 + 2];

                            var dist = GameMath.Sqrt(fromPos.SquareDistanceTo(toPos.X, toPos.Y, toPos.Z));
                            if (dist > rad) continue;

                            var blockIntercept = new BlockSelection();
                            var entityIntercept = new EntitySelection();

                            var dir1 = fromPos.SubCopy(toPos);

                            var ray1 = new Ray()
                            {
                                origin = fromPos,
                                dir = dir1
                            };

                            api.World.RayTraceForSelection(ray1, ref blockIntercept, ref entityIntercept);

                            if (blockIntercept != null && !blockIntercepts.Any(p => p.Position.Equals(blockIntercept.Position))) blockIntercepts.Add(blockIntercept);
                            if (entityIntercept != null && !entityIntercepts.Any(p => p.Position.Equals(entityIntercept.Position))) entityIntercepts.Add(entityIntercept);
                        }

                        var ember = bA.GetBlock(new AssetLocation("game:ember"));

                        foreach (var bs in blockIntercepts)
                        {
                            var block = api.World.BlockAccessor.GetBlock(bs.Position);

                            if (block.Id != 0)
                            {
                                bA.SetBlock(0, bs.Position);
                                bA.TriggerNeighbourBlockUpdate(bs.Position);
                                if (api.World.Rand.NextDouble() > 0.99) block.OnBlockExploded(api.World, bs.Position, startPos, EnumBlastType.RockBlast);
                                sploded++;
                            }
                        }

                        bA.Commit();

                        foreach (var entitySel in entityIntercepts)
                        {
                            entitySel.Entity?.ReceiveDamage(new DamageSource(), 50.0f / sploded);
                            sploded++;
                        }
                        splode++;
                    }
                    else
                    {
                        api.Event.UnregisterGameTickListener(id);
                    }
                },
                1);
            });
        }

        public bool InsideRadius(int rad, int x, int y, int z)
        {
            return (x * x + y * y + z * z) <= (rad * rad);
        }
    }
    */
}