namespace IrcClientCore.Commands
{
    internal class PartCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length > 2)
            {
                return;
            }
            else if (args.Length == 2 && args[1].StartsWith("#"))
            {
                Irc.PartChannel(args[1]);
            }
            else
            {
                Irc.PartChannel(channel);
            }
        }
    }
}