using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    [HarmonyPatch(typeof(GuiDialogCharacter), "updateEnvText")]
    class GuiDialogCharacterFix
    {
        public static void Prefix(GuiDialogCharacter __instance, ref GuiDialog.DlgComposers ___Composers)
        {
            var composer = ___Composers?["environment"];
            if (composer == null)
            {
                __instance.ComposeEnvGui();
            }
        }
    }
}
