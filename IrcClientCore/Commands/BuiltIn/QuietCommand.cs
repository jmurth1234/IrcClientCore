using System;

namespace IrcClientCore.Commands
{
    internal class QuietCommand : BaseCommand
    {
        public override void RunCommand(string[] args)
        {
            if (args.Length != 2)
            {
                ClientMessage("Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;
            if (args[0].ToLower().Contains("unmute"))
            {
                modeArgs = new string[] { "MODE", Irc.CurrentChannel, "-q", args[1] + "!*@*" };
            }
            else
            {
                modeArgs = new string[] { "MODE", Irc.CurrentChannel, "+q", args[1] + "!*@*" };
            }

            Irc.CommandManager.GetCommand("/mode").RunCommand(modeArgs);
        }
    }
}