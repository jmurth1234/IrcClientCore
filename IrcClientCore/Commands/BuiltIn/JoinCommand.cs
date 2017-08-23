namespace IrcClientCore.Commands
{
    internal class JoinCommand : BaseCommand
    {
        public override void RunCommand(string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            Irc.JoinChannel(args[1]);
        }
    }
}