using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    [HarmonyPatch(typeof(ChunkRenderer), "RenderOpaque")]
    class FastBlockHighlights
    {
        //NYI
    }
}
