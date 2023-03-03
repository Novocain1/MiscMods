using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using HarmonyLib;
using System.IO;
using Vintagestory.API.Config;
using Vintagestory.API.Common;

namespace VSHUD
{
    [HarmonyPatch(typeof(ChunkTesselator), "NowProcessChunks")]
    public class ChunkPartGrabber : ClientModSystem
    {
        public static bool Process { get => ConfigLoader.Config.CreateChunkObjs; }
        public int Seed = 0;
        public Vec3i SpawnPos;
        MassFileExportSystem fileExport;
        public override double ExecuteOrder() => 1.0;

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                string path = Path.Combine(GamePaths.DataPath, "WorldParts");
                path = Path.Combine(path, api.World.Seed.ToString(), "Textures");
                Directory.CreateDirectory(path);

                ClientMain game = (api.World as ClientMain);
                game.SetField("guiShaderProg", ShaderPrograms.Gui);
                BlockTextureAtlasManager mgr = game.GetField<BlockTextureAtlasManager>("BlockAtlasManager");

                for (int i = 0; i < mgr.Atlasses.Count; i++)
                {
                    mgr.Atlasses[i].Export(Path.Combine(path, "blockAtlas-" + i), game, mgr.AtlasTextures[i].TextureId);
                }

                fileExport = api.VSHUD().fileExport;

                Seed = api.World.Seed;
                var spawnChunk = api.World.DefaultSpawnPosition.AsBlockPos.GetChunkPos(api.World.BlockAccessor);
                SpawnPos = spawnChunk;
            };
        }

        public static void PushToStack(MeshData mesh, int chunkX, int chunkY, int chunkZ, EnumChunkRenderPass pass, int lod, bool IsEdgePiece, ChunkPartGrabber grabber)
        {
            string fileName = string.Format("{0} {1} {2} {3} lod{4}", chunkX, chunkY, chunkZ, IsEdgePiece ? "Edge" : "Center", lod);
            string filePath = Path.Combine(GamePaths.DataPath, "WorldParts");
            filePath = Path.Combine(filePath, grabber.Seed.ToString());
            filePath = Path.Combine(filePath, pass.ToString());
            Directory.CreateDirectory(filePath);
            filePath = Path.Combine(filePath, fileName + ".obj");

            long[] id = new long[] { 0, 0 };
            id[0] |= (long)chunkX;
            id[0] |= (long)chunkY << 32;
            id[1] |= (long)chunkZ;
            id[1] |= (long)pass << 32;
            id[1] |= (long)lod << 40;
            id[1] |= (long)(IsEdgePiece ? 0 : 1) << 48;

            grabber.fileExport.Push(new ExportableChunkPart(mesh, filePath, fileName, id));
        }

        public static void Postfix(ChunkTesselator __instance, int chunkX, int chunkY, int chunkZ, TesselatedChunk tessChunk)
        {
            if (!Process) return;
            var centerParts = tessChunk.GetField<TesselatedChunkPart[]>("centerParts");
            var edgeParts = tessChunk.GetField<TesselatedChunkPart[]>("edgeParts");

            if (centerParts != null) foreach(var val in centerParts) QueueUpChunkPart(val, chunkX, chunkY, chunkZ, false, __instance.GetChunkPartGrabber());
            if (edgeParts != null) foreach (var val in edgeParts) QueueUpChunkPart(val, chunkX, chunkY, chunkZ, true, __instance.GetChunkPartGrabber());
        }

        public static void QueueUpChunkPart(TesselatedChunkPart part, int chunkX, int chunkY, int chunkZ, bool IsEdgePiece, ChunkPartGrabber grabber)
        {
            if (part == null) return;

            var mesh0 = part.GetField<MeshData>("modelDataLod0")?.Clone();
            var mesh1 = part.GetField<MeshData>("modelDataLod1")?.Clone();
            var mesh2 = part.GetField<MeshData>("modelDataNotLod2Far")?.Clone();
            var mesh3 = part.GetField<MeshData>("modelDataLod2Far")?.Clone();

            var cPass = part.GetField<EnumChunkRenderPass>("pass");

            mesh0?.Translate(new Vec3f(chunkX - grabber.SpawnPos.X, chunkY - grabber.SpawnPos.Y, chunkZ - grabber.SpawnPos.Z)?.Mul(32));
            mesh1?.Translate(new Vec3f(chunkX - grabber.SpawnPos.X, chunkY - grabber.SpawnPos.Y, chunkZ - grabber.SpawnPos.Z)?.Mul(32));
            mesh2?.Translate(new Vec3f(chunkX - grabber.SpawnPos.X, chunkY - grabber.SpawnPos.Y, chunkZ - grabber.SpawnPos.Z)?.Mul(32));
            mesh3?.Translate(new Vec3f(chunkX - grabber.SpawnPos.X, chunkY - grabber.SpawnPos.Y, chunkZ - grabber.SpawnPos.Z)?.Mul(32));

            mesh0?.CompactBuffers();
            mesh1?.CompactBuffers();
            mesh2?.CompactBuffers();
            mesh3?.CompactBuffers();

            if ((mesh0?.VerticesCount ?? 0) > 0) PushToStack(mesh0, chunkX, chunkY, chunkZ, cPass, 0, IsEdgePiece, grabber);
            if ((mesh1?.VerticesCount ?? 0) > 0) PushToStack(mesh1, chunkX, chunkY, chunkZ, cPass, 1, IsEdgePiece, grabber);
            if ((mesh2?.VerticesCount ?? 0) > 0) PushToStack(mesh2, chunkX, chunkY, chunkZ, cPass, 2, IsEdgePiece, grabber);
            if ((mesh3?.VerticesCount ?? 0) > 0) PushToStack(mesh3, chunkX, chunkY, chunkZ, cPass, 3, IsEdgePiece, grabber);
        }
    }
}
