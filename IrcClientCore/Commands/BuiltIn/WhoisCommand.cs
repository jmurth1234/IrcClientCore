namespace IrcClientCore.Commands
{
    internal class WhoisCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            Irc.WhoisDestination = channel;
            Irc.WriteLine("WHOIS " + args[1]);
        }
    }
}