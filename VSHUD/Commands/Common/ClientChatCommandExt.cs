using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace VSHUD
{
    public class ClientChatCommandExt : ClientChatCommand
    {
        public ICoreClientAPI capi;

        public ClientChatCommandExt(ICoreClientAPI capi)
        {
            this.capi = capi;
            RegisterSubCommand("help", new SubCommand((a, b, c) =>
            {
                string arg = c.PopWord()?.ToLowerInvariant();
                if (arg != null && SubCommands.ContainsKey(arg))
                {
                    capi.ShowChatMessage(SubCommands[arg].Description);
                }
                else
                {
                    if (arg == null)
                    {
                        capi.ShowChatMessage(string.Format("Please provide an argument you need help with. Type .help {0} for syntax.", Command));
                    }
                    else
                    {
                        capi.ShowChatMessage(string.Format("No such argument '{0}' exists.", arg));
                    }
                }
            }, "Gets help for specified subcommand"));
        }

        public Dictionary<string, SubCommand> SubCommands = new Dictionary<string, SubCommand>();

        public override string GetSyntax()
        {
            StringBuilder bdr = new StringBuilder("[");
            
            int i = 0;
            foreach (var val in SubCommands)
            {
                bdr.Append(val.Key);
                if (i < SubCommands.Count - 1) bdr.Append('|');
                i++;
            }
            bdr.Append(']');
            return bdr.ToString();
        }

        public override string GetHelpMessage()
        {
            return string.Format("Syntax: .{0} {1}", Command, GetSyntax());
        }

        public void RegisterSubCommand(string name, SubCommand subCommand)
        {
            SubCommands[name.ToLowerInvariant()] = subCommand;
        }

        public override void CallHandler(IPlayer player, int groupId, CmdArgs args)
        {
            string arg = args.PopWord()?.ToLowerInvariant();
            if (arg != null && SubCommands.ContainsKey(arg))
            {
                SubCommands[arg].Run(player, groupId, args);
            }
            else
            {
                capi.ShowChatMessage(GetHelpMessage());
            }
        }
    }
}
