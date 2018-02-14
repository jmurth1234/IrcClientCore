namespace IrcClientCore.Commands
{
    internal class ModeCommand : BaseCommand
    {
        public override void RunCommand(string[] args)
        {
            if ((args.Length < 3) || (args.Length > 2 && !args[1].StartsWith("#")))
            {
                ClientMessage("Command too short!");
                return;
            }

            var modeLine = "MODE ";

            if (args[1].StartsWith("#"))
            {
                modeLine += args[1] + " " + args[2];

                if (args.Length == 4)
                {
                    modeLine += " " + args[3];
                }
            }
            else
            {
                modeLine += Irc.CurrentChannel + " " + args[1];

                if (args.Length == 3)
                {
                    modeLine += " " + args[2];
                }

            }

            Irc.WriteLine(modeLine);
        }
    }
}