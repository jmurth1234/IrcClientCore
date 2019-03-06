using System;
using System.Collections.Generic;
using System.Text;

namespace IrcClientCore.Commands.BuiltIn
{
    class ListCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            Irc.WriteLine("LIST");
        }
    }
}
