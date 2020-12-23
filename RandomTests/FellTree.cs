using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace RandomTests
{
    [HarmonyPatch(typeof(ItemAxe), "OnBlockBrokenWith")]
    class FellTree
    {
        public static bool Prefix()
        {
            return false;
        }

        static readonly MeshData[] LastGenerated = new MeshData[] { null, null };

        public static MeshData LastLogs { get => LastGenerated[0]; set => LastGenerated[0] = value; }
        public static MeshData LastLeaves { get => LastGenerated[1]; set => LastGenerated[1] = value; }

        public static void GenTreeMeshesAndHide(ICoreClientAPI capi, BlockPos originPos, int[] blockIDs, int[] positions, bool[] bools)
        {
            var bA = capi.World.BlockAccessor;
            var worldmap = (capi.World as ClientMain).GetField<ClientWorldMap>("WorldMap");

            MeshData logMesh = null;
            MeshData leavesMesh = null;

            BlockPos tempPos = new BlockPos();

            for (int i = 0; i < blockIDs.Length; i++)
            {
                int id = blockIDs[i];
                int x = tempPos.X = positions[i * 3 + 0];
                int y = tempPos.Y = positions[i * 3 + 1];
                int z = tempPos.Z = positions[i * 3 + 2];

                bA.SetBlock(0, tempPos);

                int xSubOrigin = x - originPos.X;
                int ySubOrigin = y - originPos.Y;
                int zSubOrigin = z - originPos.Z;

                var block = capi.World.Blocks[id];

                bool isLog = bools[i * 3 + 0];
                bool isBranchy = bools[i * 3 + 1];
                bool isLeaves = bools[i * 3 + 2];

                if (capi.TesselatorManager.TesselateBlockAdv(block, out MeshData addMesh, x, y, z))
                {
                    if (isLog)
                    {
                        if (logMesh == null) logMesh = addMesh;
                        else
                        {
                            addMesh.Translate(xSubOrigin, ySubOrigin, zSubOrigin);
                            logMesh.AddMeshData(addMesh);
                        }
                    }
                    else if (isLeaves || isBranchy)
                    {
                        ColorMapData colormap = worldmap.getColorMapData(block, x, y, z);

                        addMesh.CustomInts = new CustomMeshDataPartInt()
                        {
                            InterleaveOffsets = new int[addMesh.VerticesCount],
                            InterleaveSizes = new int[addMesh.VerticesCount],
                            InterleaveStride = 8,
                            Conversion = DataConversion.Integer
                        };

                        for (int j = 0; j < addMesh.VerticesCount; j++)
                        {
                            addMesh.CustomInts.InterleaveOffsets[j] = j * 4;
                            addMesh.CustomInts.InterleaveSizes[j] = 2;
                            addMesh.CustomInts.Add(colormap.Value);
                        }

                        if (logMesh != null) addMesh.Translate(xSubOrigin, ySubOrigin, zSubOrigin);

                        if (leavesMesh == null) leavesMesh = addMesh;
                        else leavesMesh.AddMeshData(addMesh);
                    }
                }
            }
            
            logMesh?.CompactBuffers();
            leavesMesh?.CompactBuffers();

            LastLogs = logMesh;
            LastLeaves = leavesMesh;
        }

        public static void SpawnLeaves(IClientWorldAccessor world, SimpleParticleProperties dustParticles, Block block, BlockPos pos, float windSpeed)
        {
            dustParticles.Color = block.GetRandomColor(world.Api as ICoreClientAPI, pos, BlockFacing.UP);
            dustParticles.Color |= 255 << 24;
            dustParticles.MinPos.Set(pos.X, pos.Y, pos.Z);

            if (block.BlockMaterial == EnumBlockMaterial.Leaves)
            {
                dustParticles.GravityEffect = (float)world.Rand.NextDouble() * 0.1f + 0.01f;
                dustParticles.ParticleModel = EnumParticleModel.Quad;
                dustParticles.MinVelocity.Set(-0.4f + 4 * (float)windSpeed, -0.4f, -0.4f);
                dustParticles.AddVelocity.Set(0.8f + 4 * (float)windSpeed, 1.2f, 0.8f);
            }
            else
            {
                dustParticles.GravityEffect = 0.8f;
                dustParticles.ParticleModel = EnumParticleModel.Cube;
                dustParticles.MinVelocity.Set(-0.4f + (float)windSpeed, -0.4f, -0.4f);
                dustParticles.AddVelocity.Set(0.8f + (float)windSpeed, 1.2f, 0.8f);
            }

            world.SpawnParticles(dustParticles);
        }

        public static void Postfix(ItemAxe __instance, ref bool __result, IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier)
        {
            var capi = world.Api as ICoreClientAPI;

            float fallTime = 2.0f;

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            double windspeed = world.Api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) ?? 0;


            string treeType;
            Stack<BlockPos> foundPositions = __instance.FindTree(world, blockSel.Position, out treeType);

            Block leavesBranchyBlock = world.GetBlock(new AssetLocation("leavesbranchy-grown-" + treeType));
            Block leavesBlock = world.GetBlock(new AssetLocation("leaves-grown-" + treeType));


            if (foundPositions.Count == 0)
            {
                __result = new Item().OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
                return;
            }

            var chopFace = blockSel.Face;
            var fallDirection = chopFace.IsHorizontal ? chopFace.Opposite : BlockFacing.HORIZONTALS_ANGLEORDER[world.Rand.Next(0, 3)];

            bool damageable = __instance.DamagedBy != null && __instance.DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);

            float leavesMul = 1;
            float leavesBranchyMul = 0.8f;
            int blocksbroken = 0;

            Stack<KeyValuePair<Block, float>> blocks = new Stack<KeyValuePair<Block, float>>();
            BlockPos originPos = blockSel.Position;
            Stack<BlockPos> breakable = new Stack<BlockPos>();

            Stack<int> blockIds = new Stack<int>();
            Stack<int> positions = new Stack<int>();
            Stack<bool> bools = new Stack<bool>();

            while (foundPositions.Count > 0)
            {
                BlockPos pos = foundPositions.Pop();
                breakable.Push(pos);
                blocksbroken++;

                Block block = world.BlockAccessor.GetBlock(pos);

                bool isLog = block.Code.Path.StartsWith("beehive-inlog-" + treeType) || block.Code.Path.StartsWith("log-resinharvested-" + treeType) || block.Code.Path.StartsWith("log-resin-" + treeType) || block.Code.Path.StartsWith("log-grown-" + treeType) || block.Code.Path.StartsWith("bamboo-grown-brown-segment") || block.Code.Path.StartsWith("bamboo-grown-green-segment");
                bool isBranchy = block == leavesBranchyBlock;
                bool isLeaves = block == leavesBlock || block.Code.Path == "bambooleaves-grown";

                bools.Push(isLeaves);
                bools.Push(isBranchy);
                bools.Push(isLog);

                blockIds.Push(block.Id);

                positions.Push(pos.Z);
                positions.Push(pos.Y);
                positions.Push(pos.X);

                blocks.Push(new KeyValuePair<Block, float>(block, isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1)));

                if (world.Side == EnumAppSide.Client)
                {
                    var dustParticles = __instance.GetField<SimpleParticleProperties>("dustParticles");
                    SpawnLeaves((IClientWorldAccessor)world, dustParticles, block, pos, (float)windspeed);
                }

                if (damageable && isLog)
                {
                    __instance.DamageItem(world, byEntity, itemslot);
                }

                if (itemslot.Itemstack == null) {
                    __result = true;
                    return;
                }

                if (isLeaves && leavesMul > 0.03f) leavesMul *= 0.85f;
                if (isBranchy && leavesBranchyMul > 0.015f) leavesBranchyMul *= 0.6f;
            }
            
            var indexable = breakable.ToArray();

            if (world.Side.IsClient())
            {
                GenTreeMeshesAndHide(capi, originPos, blockIds.ToArray(), positions.ToArray(), bools.ToArray());

                if (LastLogs != null)
                {
                    new FallingTreeRenderer(capi, originPos, false, LastLogs, fallTime, fallDirection, EnumRenderStage.Opaque);
                    new FallingTreeRenderer(capi, originPos, false, LastLogs, fallTime, fallDirection, EnumRenderStage.ShadowNear);
                    new FallingTreeRenderer(capi, originPos, false, LastLogs, fallTime, fallDirection, EnumRenderStage.ShadowFar);
                }

                if (LastLeaves != null)
                {
                    new FallingTreeRenderer(capi, originPos, true, LastLeaves, fallTime, fallDirection, EnumRenderStage.Opaque);
                    new FallingTreeRenderer(capi, originPos, true, LastLeaves, fallTime, fallDirection, EnumRenderStage.ShadowNear);
                    new FallingTreeRenderer(capi, originPos, true, LastLeaves, fallTime, fallDirection, EnumRenderStage.ShadowFar);
                }
            }

            world.RegisterCallback((dt) => {
                int i = 0;
                foreach (var toBreak in blocks)
                {
                    toBreak.Key.OnBlockBroken(world, indexable[i], byPlayer, toBreak.Value);
                    world.BlockAccessor.TriggerNeighbourBlockUpdate(indexable[i]);
                    i++;
                }

            }, (int)(fallTime * 1000.0f));

            if (blocksbroken > 1)
            {
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
                world.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), pos.X, pos.Y, pos.Z, byPlayer, false, 32, GameMath.Clamp(blocksbroken / 100f, 0.25f, 1));
            }

            __result = true;
        }
    }
}
