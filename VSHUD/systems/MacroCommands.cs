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
    class MacroCommands : ClientModSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            MacroManagerRef macroManager = new MacroManagerRef(AccessTools.Field(typeof(ClientMain), "macroManager").GetValue(api.World as ClientMain) as MacroManager);

            api.RegisterCommand("addmacro", "adds macro from string", ".addmacro alt ctrl shift keycode secondkeycode^macroname^first command^second command^nth command", (a, args) =>
            {
                try
                {
                    string[] arguments = args.PopAll().Split('^');

                    string hotkey = arguments.First();
                    List<string> commands = arguments.ToList();
                    commands.RemoveAt(0);

                    string code = commands.First();
                    commands.RemoveAt(0);

                    CmdArgs newArgs = new CmdArgs(hotkey);

                    macroManager.AddMacro(new Macro()
                    {
                        Code = code,
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
                    });
                }
                catch (Exception)
                {
                    api.World.Player.ShowChatNotification("Syntax: \".addmacro alt ctrl shift keycode secondkeycode^macroname^first command^second command^nth command\"");
                }

            });

            api.RegisterCommand("deletemacro", "deletes macro with the specified index", ".deletemacro (index)", (a, args) => 
            {
                int? index = args.PopInt();
                if (index != null) macroManager.DeleteMacro((int)index);
                else api.World.Player.ShowChatNotification("syntax: \".deletemacro (index)\"");
            });

            api.RegisterCommand("listmacros", "lists registered macros", "", (a, args) =>
            {
                StringBuilder stringBuilder = new StringBuilder("Macros:").AppendLine();

                int i = 1;

                foreach (var macro in macroManager.Macros)
                {
                    stringBuilder.Append(string.Format("{0}: {1}", macro.Key, macro.Value.Name));
                    if (i <= macroManager.MacroCount - 1) stringBuilder.AppendLine();
                    i++;
                }
                api.World.Player.ShowChatNotification(stringBuilder.ToString());
            });
        }

        GlKeys GetGlKey(ref CmdArgs args)
        {
            Enum.TryParse(args.PopWord(), true, out GlKeys key);
            return key;
        }
    }
    
    class MacroManagerRef
    {
        public int MacroCount { get => Macros.Count; }

        MacroManager macroManager;
        
        public SortedDictionary<int, Macro> Macros { get => macroManager.MacrosByIndex; }

        public MacroManagerRef(MacroManager macroManager)
        {
            this.macroManager = macroManager;   
        }

        public void AddMacro(Macro macro)
        {
            SetMacro(20 + Macros.Count + 1, macro);
        }

        public void DeleteMacro(int index) => macroManager.DeleteMacro(index);

        public void SetMacro(int macroIndex, Macro macro) => macroManager.SetMacro(macroIndex, macro);

    }
    
}
