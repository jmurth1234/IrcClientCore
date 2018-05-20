using System;
using System.Collections.Generic;
using System.Text;

namespace IrcClientCore.Commands
{
    class HelpCommand : BaseCommand
    { 
        public override void RunCommand(string channel, string[] args)
        {
            ClientMessage(channel, "The following commands are available: ");
            ClientMessage(channel, string.Join(", ", Irc.CommandManager.CommandList));
        }
    }
}
