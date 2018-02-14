using System;

namespace IrcClientCore.Commands
{
    internal class BanCommand : BaseCommand
    {
        public override void RunCommand(string[] args)
        {
            if (args.Length != 2)
            {
                ClientMessage("Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;

            modeArgs = new string[] { "MODE", Irc.CurrentChannel, "+b", args[1] + "!*@*" };

            Irc.CommandManager.GetCommand("/mode").RunCommand(modeArgs);
        }
    }
}