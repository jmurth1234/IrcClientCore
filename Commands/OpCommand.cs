using System;

namespace IrcClientCore.Commands
{
    internal class OpCommand : BaseCommand
    {
        public override void RunCommand(string[] args)
        {
            if (args.Length != 2)
            {
                Irc.ClientMessage("Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;
            if (args[0].ToLower().Contains("deop"))
            {
                modeArgs = new string[] { "MODE", Irc.currentChannel, "-o", args[1] };
            }
            else
            {
                modeArgs = new string[] { "MODE", Irc.currentChannel, "+o", args[1] };
            }

            Irc.CommandManager.GetCommand("/mode").RunCommand(modeArgs);
        }
    }
}