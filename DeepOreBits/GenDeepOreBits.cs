using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        ICoreServerAPI Api;
        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;
        public override double ExecuteOrder() => 0.9;
        LCGRandom rnd;
        IWorldGenBlockAccessor bA;
        Dictionary<int, int> surfaceBlocks = new Dictionary<int, int>();
        List<DeepOreGenProperty> genProperties;

        public override void StartServerSide(ICoreServerAPI Api)
        {
            this.Api = Api;
            if (DoDecorationPass)
            {
                foreach (var block in Api.World.Blocks)
                {
                    if (block is BlockOre)
                    {
                        int? id = Api.World.BlockAccessor.GetBlock(new AssetLocation("looseores".Apd(block.Variant["type"]).Apd(block.Variant["rock"])))?.Id;
                        if (id != null)
                        {
                            surfaceBlocks.Add(block.Id, (int)id);
                        }
                    }
                }
                genProperties = Api.Assets.Get("deeporebits:worldgen/deeporebits.json").ToObject<List<DeepOreGenProperty>>();

                foreach (var val in genProperties)
                {
                    val.id = Api.World.GetBlock(val.Code).Id;
                }

                Api.Event.InitWorldGenerator(InitWorldGen, "standard");
                Api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
                Api.Event.GetWorldgenBlockAccessor(c => bA = c.GetBlockAccessor(true));
            }
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
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
                        double chance = 1.0;
                        genProperties.Any(d =>
                        {
                            if (d.id == surface)
                            {
                                chance = d.Chance;
                                return true;
                            }
                            return false;
                        });

                        if (rnd.NextDouble() > chance) continue;

                        for (int y = vec.Y; y < chunksize; y++)
                        {
                            rnd.InitPositionSeed(chunkX * vec.X, chunkZ * vec.Z);
                            if (y < 1 || rnd.NextDouble() > 0.1) continue;
                            int dX = rnd.NextInt(chunksize), dZ = rnd.NextInt(chunksize);

                            int block = chunk.Blocks[(y * chunksize + dZ) * chunksize + dX];
                            int dBlock = chunk.Blocks[((y - 1) * chunksize + dZ) * chunksize + dX];
                            if (bA.GetBlock(dBlock).Fertility > 4 && bA.GetBlock(block).IsReplacableBy(bA.GetBlock(ore)) && !bA.GetBlock(block).IsLiquid())
                            {
                                chunk.Blocks[(y * chunksize + dZ) * chunksize + dX] = surface;
                                bA.ScheduleBlockUpdate(new BlockPos(dX, y, dZ));
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void InitWorldGen()
        {
            LoadGlobalConfig(Api);
            rnd = new LCGRandom(Api.WorldManager.Seed);
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    class DeepOreGenProperty
    {
        public DeepOreGenProperty(AssetLocation code, float chance)
        {
            Code = code;
            Chance = chance;
        }

        public int id;

        [JsonProperty]
        public AssetLocation Code { get; set; }

        [JsonProperty]
        public double Chance { get; set; }
    }
}
