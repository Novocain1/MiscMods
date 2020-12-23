using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace RandomTests
{
    public static class Extensions
    {
        public static bool TesselateBlockAdv(this ITesselatorManager tesselator, Block block, out MeshData tesselated, int x = 0, int y = 0, int z = 0)
        {
            int id = block.Id;

            var tesselatormanager = tesselator as ShapeTesselatorManager;

            var lod0 = (tesselatormanager.blockModelDatasLod0.ContainsKey(id) ? tesselatormanager.blockModelDatasLod0[id] : null)?.Clone();
            var lod1 = tesselatormanager.blockModelDatas[id].Clone();
            var lod0alt = tesselatormanager.altblockModelDatasLod0[id];
            var lod1alt = tesselatormanager.altblockModelDatasLod1[id];

            if (block.HasAlternates && lod1alt != null)
            {
                long alternateIndex = block.RandomizeAxes == EnumRandomizeAxes.XYZ ? GameMath.MurmurHash3Mod(x, y, z, lod1alt.Length) : GameMath.MurmurHash3Mod(x, 0, z, lod1alt.Length);
                tesselated = lod1alt[alternateIndex].Clone();
                var lod = lod0alt?[alternateIndex].Clone();

                if (lod != null && tesselated != lod)
                {
                    tesselated.IndicesMax = tesselated.Indices.Count();
                    lod.IndicesMax = lod.Indices.Count();

                    tesselated.AddMeshData(lod);
                }
            }
            else
            {
                tesselated = lod1;
                tesselated.IndicesMax = tesselated.Indices.Count();
                if (lod0 != null)
                {
                    lod0.IndicesMax = lod0.Indices.Count();
                    if (tesselated != lod0) tesselated.AddMeshData(lod0);
                }
            }

            tesselated?.CompactBuffers();

            return tesselated != null;
        }
    }
}
