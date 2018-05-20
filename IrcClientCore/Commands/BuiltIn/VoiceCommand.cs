using System;

namespace IrcClientCore.Commands
{
    internal class VoiceCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length != 2)
            {
                ClientMessage(channel, "Wrong params: " + string.Join(" ", args));
                return;
            }

            string[] modeArgs;
            if (args[0].ToLower().Contains("devoice"))
            {
                modeArgs = new string[] { "MODE", channel, "-v", args[1] };
            }
            else
            {
                modeArgs = new string[] { "MODE", channel, "+v", args[1] };
            }

            Irc.CommandManager.GetCommand(channel, "/mode").RunCommand(channel, modeArgs);
        }
    }
}