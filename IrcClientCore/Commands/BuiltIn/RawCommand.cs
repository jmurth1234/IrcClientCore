using System;

namespace IrcClientCore.Commands
{
    internal class RawCommand : BaseCommand
    {
        public override void RunCommand(string[] args)
        {
            if (args.Length == 1)
            {
                return;
            }
            var message = String.Join(" ", args, 1, args.Length - 1);
            Irc.WriteLine(message);
        }
    }
}