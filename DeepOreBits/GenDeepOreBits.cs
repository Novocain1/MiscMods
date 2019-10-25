using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace DeepOreBits
{
    class GenDeepOreBits : ModStdWorldGen
    {
        ICoreServerAPI api;
        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;
        public override double ExecuteOrder() => 0.9;
        NormalizedSimplexNoise noise;
        IWorldGenBlockAccessor bA;
        public Dictionary<int, int> surfaceBlocks = new Dictionary<int, int>();

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            if (DoDecorationPass)
            {
                foreach (var block in api.World.Blocks)
                {
                    if (block is BlockOre)
                    {
                        int? id = api.World.BlockAccessor.GetBlock(new AssetLocation("looseores".Apd(block.Variant["type"]).Apd(block.Variant["rock"])))?.Id;
                        if (id != null)
                        {
                            surfaceBlocks.Add(block.Id, (int)id);
                        }
                    }
                }
                api.Event.InitWorldGenerator(InitWorldGen, "standard");
                api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
                api.Event.GetWorldgenBlockAccessor(c => bA = c.GetBlockAccessor(true));
            }
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            /*
            Dictionary<Vec3i, int> ores = new Dictionary<Vec3i, int>();
            for (int cY = 0; cY < chunks.Length; cY++)
            {
                IServerChunk chunk = chunks[cY];
                if (chunk.Blocks == null) continue;
                for (int x = 0; x < chunksize; x++)
                {
                    for (int y = 0; y < chunksize; y++)
                    {
                        for (int z = 0; z < chunksize; z++)
                        {
                            int block = chunk.Blocks[(y * chunksize + z) * chunksize + x];
                            if (surfaceBlocks.ContainsKey(block) && !ores.ContainsKey(new Vec3i(x, y, z)))
                            {
                                ores.Add(new Vec3i(x, y, z), block);
                            }
                        }
                    }
                }
                foreach (var val in ores)
                {
                    Vec3i vec = val.Key;
                    int ore = val.Value;
                    if (surfaceBlocks.TryGetValue(ore, out int surface))
                    {
                        for (int y = vec.Y; y < chunksize; y++)
                        {
                            rnd.InitPositionSeed(chunkX * vec.X, chunkZ * vec.Z);
                            if (y < 1 || rnd.NextDouble() > 0.1) continue;
                            int dX = rnd.NextInt(chunksize - 1), dZ = rnd.NextInt(chunksize - 1);

                            int block = chunk.Blocks[(y * chunksize + dZ) * chunksize + dX];
                            int dBlock = chunk.Blocks[((y - 1) * chunksize + dZ) * chunksize + dX];
                            if (bA.GetBlock(dBlock).Fertility > 4 && bA.GetBlock(block).IsReplacableBy(bA.GetBlock(ore)) && !bA.GetBlock(block).IsLiquid())
                            {
                                chunk.Blocks[(y * chunksize + dZ) * chunksize + dX] = surface;
                                bA.ScheduleBlockUpdate(new BlockPos(dX, y, dZ));
                            }
                        }
                    }
                }
            }
            */
        }

        public void InitWorldGen()
        {
            LoadGlobalConfig(api);
            noise = NormalizedSimplexNoise.FromDefaultOctaves(TerraGenConfig.terrainGenOctaves, 0.002, 0.9, api.WorldManager.Seed);
        }
    }
}
