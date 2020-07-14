using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    class MacroCommands : ModSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            MacroManagerRef macroManager = new MacroManagerRef(AccessTools.Field(typeof(ClientMain), "macroManager").GetValue(api.World as ClientMain) as _0Tm6DA0rKiqFcpnbrNpbCC2k62N);

            api.RegisterCommand("addmacro", "adds macro from string", ".addmacro alt ctrl shift keycode secondkeycode^first command^second command^nth command", (a, args) =>
            {
                string[] arguments = args.PopAll().Split('^');
                string hotkey = arguments.First();
                List<string> commands = arguments.ToList();
                commands.RemoveAt(0);
                
                CmdArgs newArgs = new CmdArgs(hotkey);

                macroManager.AddMacro(new Macro() 
                { 
                    Code = (macroManager.MacroCount + 1).ToString(),
                    Commands = commands.ToArray(),
                    Index = macroManager.MacroCount + 1,
                    KeyCombination = new KeyCombination() 
                    { 
                        Alt = newArgs.PopBool() ?? false,
                        Ctrl = newArgs.PopBool() ?? false,
                        Shift = newArgs.PopBool() ?? false,
                        KeyCode = (int)GetGlKey(ref newArgs),
                        SecondKeyCode = (int)GetGlKey(ref newArgs),
                    }
                });;
            });
        }

        GlKeys GetGlKey(ref CmdArgs args)
        {
            Enum.TryParse(args.PopWord(), true, out GlKeys key);
            return key;
        }
    }

    class Macro : _2W3ASXNTsjoMArf6vsoIsP7Fwjj
    { 
    }

    class MacroManagerRef
    {
        public int MacroCount { get => macroManager._1lc7UoG0olMZTp2U4kkPPyTxm0C.Count; }

        _0Tm6DA0rKiqFcpnbrNpbCC2k62N macroManager;

        public MacroManagerRef(_0Tm6DA0rKiqFcpnbrNpbCC2k62N macroManager)
        {
            this.macroManager = macroManager;   
        }

        public void AddMacro(Macro macro)
        {
            SetMacro(macroManager._1lc7UoG0olMZTp2U4kkPPyTxm0C.Count + 1, macro);
        }

        public void SetMacro(int macroIndex, Macro macro) => macroManager._9VyhV5EjOJ8EOmzFhkQwsPEyMF(macroIndex, macro);

    }
}
