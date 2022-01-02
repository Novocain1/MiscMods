using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace VSHUD
{
    class VanillaPatches : ClientModSystem
    {
        public const string patchCode = "Novocain.ModSystem.VanillaPatches";
        public Harmony harmonyInstance = new Harmony(patchCode);

        public override void StartClientSide(ICoreClientAPI api)
        {
            harmonyInstance.PatchAll();
            StringBuilder builder = new StringBuilder("Harmony Patched Methods: ");
            foreach (var val in harmonyInstance.GetPatchedMethods())
            {
                builder.Append(val.Name + ", ");
            }
            api.Logger.Notification(builder.ToString());
        }

        public override void Dispose()
        {
            harmonyInstance.UnpatchAll(patchCode);
        }
    }
}
