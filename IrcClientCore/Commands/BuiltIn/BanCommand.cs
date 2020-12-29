using System;

namespace IrcClientCore.Commands
{
    internal class TopicCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length < 2)
            {
                _ = Irc.WriteLine($"TOPIC {channel}");

                return;
            }

            var topic = string.Join(" ", args, 1, args.Length - 1);

            _ = Irc.WriteLine($"TOPIC {channel} :{topic}");
        }
    }
}