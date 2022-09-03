using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VSHUD
{
    internal class HarmonyPatcher : ModSystem
    {
        private const string patchCode = "ModSystem";
        public string sidedPatchCode;

        public Harmony harmonyInstance;
        private static bool harmonyPatched;

        public override void Start(ICoreAPI api)
        {
            if (harmonyPatched) return;
            var asm = Assembly.GetExecutingAssembly();
            string modName = Mod?.Info.Name ?? asm.GetCustomAttribute<ModInfoAttribute>()?.Name ?? "Null";

            sidedPatchCode = string.Format("{0}.{1}.{2}", modName, patchCode, api.Side);
            harmonyInstance = new Harmony(sidedPatchCode);
#if DEBUG
            Harmony.DEBUG = true;
#endif
            harmonyInstance.PatchAll();

            Dictionary<string, int> counts = new Dictionary<string, int>();

            foreach (var val in harmonyInstance.GetPatchedMethods())
            {
                if (counts.ContainsKey(val.FullDescription()))
                {
                    counts[val.FullDescription()]++;
                }
                else counts[val.FullDescription()] = 1;
            }

            StringBuilder builder = new StringBuilder(string.Format("{0}: Harmony Patched Methods: ", modName)).AppendLine();

            builder.AppendLine("[");
            foreach (var method in counts)
            {
                builder.AppendLine(string.Format("  {0}: {1}", method.Value, method.Key));
            }
            builder.Append("]");

            api.Logger.Notification(builder.ToString());

            harmonyPatched = true;
        }

        public override void Dispose()
        {
            harmonyInstance?.UnpatchAll(sidedPatchCode);
            harmonyPatched = false;
        }
    }
}
