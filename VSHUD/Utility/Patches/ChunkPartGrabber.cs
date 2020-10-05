using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using HarmonyLib;
using System.IO;
using Vintagestory.API.Config;

namespace VSHUD
{
    [HarmonyPatch(typeof(ChunkTesselator), "NowProcessChunks")]
    class ChunkPartGrabber
    {
        public static MeshData Combined = new MeshData(1, 1);
        public static bool Process { get => ConfigLoader.Config.CreateChunkObjs; }
        public static int Seed = 0;
        public static Vec3i SpawnPos;

        public static void PushToStack(MeshData mesh, int chunkX, int chunkY, int chunkZ, EnumChunkRenderPass pass, int lod)
        {
            string fileName = string.Format("{0} {1} {2} {3} lod{4}", chunkX, chunkY, chunkZ, pass, lod);
            string filePath = Path.Combine(GamePaths.Binaries, "worldparts");
            filePath = Path.Combine(filePath, Seed.ToString());
            Directory.CreateDirectory(filePath);
            filePath = Path.Combine(filePath, fileName + ".obj");

            MassFileExportSystem.toExport.Push(new PreparedMesh(mesh, filePath, fileName));
        }
        
        public static void Postfix(int chunkX, int chunkY, int chunkZ, ref TesselatedChunkPart[] __result)
        {
            if (!Process) return;

            int i = 0;
            foreach (var val in __result)
            {
                if (val == null) continue;
                
                var mesh0 = val.GetField<MeshData>("modelDataLod0").Clone();
                var mesh1 = val.GetField<MeshData>("modelDataLod1").Clone();
                var cPass = val.GetField<EnumChunkRenderPass>("pass");

                mesh0.Translate(new Vec3f(chunkX - SpawnPos.X, chunkY - SpawnPos.Y, chunkZ - SpawnPos.Z).Mul(32));
                mesh1.Translate(new Vec3f(chunkX - SpawnPos.X, chunkY - SpawnPos.Y, chunkZ - SpawnPos.Z).Mul(32));
                mesh0.CompactBuffers();
                mesh1.CompactBuffers();

                if (mesh0.VerticesCount > 0) PushToStack(mesh0, chunkX, chunkY, chunkZ, cPass, 0);
                if (mesh1.VerticesCount > 0) PushToStack(mesh1, chunkX, chunkY, chunkZ, cPass, 1);

                i++;
            }
        }
    }
}
