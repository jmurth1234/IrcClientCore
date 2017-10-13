using System;

namespace IrcClientCore.Commands
{
    internal class VoiceCommand : BaseCommand
    {
        public override void RunCommand(string[] args)
        {
            if (args.Length != 2)
            {
                ClientMessage("Wrong params: " + String.Join(" ", args));
                return;
            }

            string[] modeArgs;
            if (args[0].ToLower().Contains("devoice"))
            {
                modeArgs = new string[] { "MODE", Irc.CurrentChannel, "-v", args[1] };
            }
            else
            {
                modeArgs = new string[] { "MODE", Irc.CurrentChannel, "+v", args[1] };
            }

            Irc.CommandManager.GetCommand("/mode").RunCommand(modeArgs);
        }
    }
}