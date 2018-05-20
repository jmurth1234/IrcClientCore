using System;

namespace IrcClientCore.Commands
{
    internal class MsgCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length < 3)
            {
                return;
            }

            var nick = args[1];
            var msg = "PRIVMSG " + nick;

            msg += " :" + string.Join(" ", args, 2, args.Length - 2);

            Irc.WriteLine(msg);
        }
    }
}