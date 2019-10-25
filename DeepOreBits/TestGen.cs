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
    class TestGen : ModStdWorldGen
    {
        ICoreServerAPI api;
        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;
        public override double ExecuteOrder() => 0.9;
        NormalizedSimplexNoise noise;
        IWorldGenBlockAccessor bA;
        public Dictionary<int, int> sandBlocks = new Dictionary<int, int>();
        public int mud;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            if (DoDecorationPass)
            {
                foreach (var block in api.World.Blocks)
                {
                    if (block.FirstCodePart() == "rock")
                    {
                        int? id = api.World.BlockAccessor.GetBlock(new AssetLocation("sand".Apd(block.Variant["rock"])))?.Id;
                        if (id != null)
                        {
                            sandBlocks.Add(block.Id, (int)id);
                        }
                    }
                }
                mud = api.World.BlockAccessor.GetBlock(new AssetLocation("muddygravel")).Id;

                api.Event.InitWorldGenerator(InitWorldGen, "standard");
                api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
                api.Event.GetWorldgenBlockAccessor(c => bA = c.GetBlockAccessor(true));
            }
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            IMapChunk mapChunk = chunks[0].MapChunk;
            for (int lx = 0; lx < chunksize; lx++)
            {
                for (int lz = 0; lz < chunksize; lz++)
                {
                    int posY = mapChunk.WorldGenTerrainHeightMap[lz * chunksize + lx];
                    int blockId = chunks[posY / chunksize].Blocks[((posY % chunksize) * chunksize + lz) * chunksize + lx];
                    if (blockId == GlobalConfig.waterBlockId)
                    {
                        int rad = 8;
                        for (int dx = lx - rad; dx < lx + rad; dx++)
                        {
                            for (int dy = posY - rad; dy < posY + 1; dy++)
                            {
                                for (int dz = lz - rad; dz < lz + rad; dz++)
                                {
                                    if ((dx * dx + dy * dy + dz * dz) < dx * dz * dy * rad)
                                    {
                                        int deltaBlock = chunks[dy / chunksize].Blocks[((dy % chunksize) * chunksize + dz) * chunksize + dx];
                                        if (deltaBlock != 0 && deltaBlock != GlobalConfig.waterBlockId && deltaBlock != mud && bA.GetBlock(deltaBlock).Fertility > 1)
                                        {
                                            chunks[dy / chunksize].Blocks[((dy % chunksize) * chunksize + dz) * chunksize + dx] = GetSandInt(mapChunk, blockId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public int GetSandInt(IMapChunk mapChunk, int deltaBlock)
        {
            if (sandBlocks.TryGetValue(mapChunk.TopRockIdMap[0], out int sand))
            {
                return sand;
            }
            return deltaBlock;
        }

        public void InitWorldGen()
        {
            LoadGlobalConfig(api);
            noise = NormalizedSimplexNoise.FromDefaultOctaves(TerraGenConfig.terrainGenOctaves, 0.002, 0.9, api.WorldManager.Seed);
        }
    }
}
