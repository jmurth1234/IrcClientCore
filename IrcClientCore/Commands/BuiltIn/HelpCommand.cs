using System;
using System.Collections.Generic;
using System.Text;

namespace IrcClientCore.Commands
{
    class HelpCommand : BaseCommand
    { 
        public override void RunCommand(string[] args)
        {
            ClientMessage("The following commands are available: ");
            ClientMessage(String.Join(", ", Irc.CommandManager.CommandList));
        }
    }
}
