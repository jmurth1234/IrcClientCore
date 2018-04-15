namespace IrcClientCore.Commands
{
    internal class QueryCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            Irc.AddChannel(args[1]);
        }
    }
}