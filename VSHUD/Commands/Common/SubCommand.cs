using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace VSHUD
{
    public class SubCommand
    {
        public SubCommand(Vintagestory.API.Common.Action<IPlayer, int, CmdArgs> command, string description, Dictionary<string, SubCommand> subCommands = null)
        {
            Command = command;
            Description = description;
            SubCommands = subCommands ?? new Dictionary<string, SubCommand>();
        }

        public Dictionary<string, SubCommand> SubCommands { get; set; }

        public Vintagestory.API.Common.Action<IPlayer, int, CmdArgs> Command { get; set; }
        public string Description { get; set; }

        public void Run(IPlayer player, int groupId, CmdArgs args)
        {
            string arg = args.PopWord()?.ToLowerInvariant();
            if (arg != null)
            {
                if (SubCommands.ContainsKey(arg))
                {
                    SubCommands[arg].Run(player, groupId, args);
                    return;
                }
                else
                {
                    args.PushSingle(arg);
                }
            }
            Command.Invoke(player, groupId, args);
        }

        public void RegisterSubCommand(string name, SubCommand subCommand)
        {
            SubCommands[name.ToLowerInvariant()] = subCommand;
        }
    }
}
