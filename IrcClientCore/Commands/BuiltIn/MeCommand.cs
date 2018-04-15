using System;
using System.Collections.Generic;
using System.Text;

namespace IrcClientCore.Commands
{
    class MeCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length == 1)
            {
                return;
            }

            var message = String.Join(" ", args, 1, args.Length - 1);
            Irc.SendAction(channel, message);
        }
    }
}
