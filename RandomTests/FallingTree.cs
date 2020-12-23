using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods;

namespace RandomTests
{
    public class FallingTree : ModSystem
    {
        public const string PatchCode = "RandomTests.Modsystem.FallingTree";
        public Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            harmony = new Harmony(PatchCode);
            harmony.PatchAll();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                api.Shader.ReloadShaders();
            };
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(PatchCode);
        }
    }

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

    public static class HackMan
    {
        public static T GetField<T>(this object instance, string fieldname) => (T)AccessTools.Field(instance.GetType(), fieldname).GetValue(instance);
        public static T GetProperty<T>(this object instance, string fieldname) => (T)AccessTools.Property(instance.GetType(), fieldname).GetValue(instance);
        public static T CallMethod<T>(this object instance, string method, params object[] args) => (T)AccessTools.Method(instance.GetType(), method).Invoke(instance, args);
        public static void CallMethod(this object instance, string method, params object[] args) => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, args);
        public static void CallMethod(this object instance, string method) => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, null);
        public static object CreateInstance(this Type type) => AccessTools.CreateInstance(type);
        public static T[] GetFields<T>(this object instance)
        {
            List<T> fields = new List<T>();
            var declaredFields = AccessTools.GetDeclaredFields(instance.GetType())?.Where((t) => t.FieldType == typeof(T));
            foreach (var val in declaredFields)
            {
                fields.Add(instance.GetField<T>(val.Name));
            }
            return fields.ToArray();
        }

        public static void SetField(this object instance, string fieldname, object setVal) => AccessTools.Field(instance.GetType(), fieldname).SetValue(instance, setVal);
        public static MethodInfo GetMethod(this object instance, string method) => AccessTools.Method(instance.GetType(), method);
    }
}
