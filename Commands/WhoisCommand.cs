namespace IrcClientCore.Commands
{
    internal class WhoisCommand : BaseCommand
    {
        public override void RunCommand(string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            Irc.WriteLine("WHOIS " + args[1]);
        }
    }
}