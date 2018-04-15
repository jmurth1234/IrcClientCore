using System;

namespace IrcClientCore.Commands
{
    internal class OpCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length != 2)
            {
                ClientMessage(channel, "Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;
            if (args[0].ToLower().Contains("deop"))
            {
                modeArgs = new string[] { "MODE", channel, "-o", args[1] };
            }
            else
            {
                modeArgs = new string[] { "MODE", channel, "+o", args[1] };
            }

            Irc.CommandManager.GetCommand(channel, "/mode").RunCommand(channel, modeArgs);
        }
    }
}