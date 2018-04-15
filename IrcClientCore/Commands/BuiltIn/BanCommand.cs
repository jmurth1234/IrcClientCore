using System;

namespace IrcClientCore.Commands
{
    internal class BanCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length != 2)
            {
                ClientMessage(channel, "Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;

            modeArgs = new string[] { "MODE", channel, "+b", args[1] + "!*@*" };

            Irc.CommandManager.GetCommand(channel, "/mode").RunCommand(channel, modeArgs);
        }
    }
}