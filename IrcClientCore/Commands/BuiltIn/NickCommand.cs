namespace IrcClientCore.Commands
{
    internal class NickCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            Irc.Nickname = args[1];
        }
    }
}