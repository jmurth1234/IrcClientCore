using System;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    internal class JoinHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var channel = parsedLine.TrailMessage.TrailingContent;
            if (parsedLine.PrefixMessage.Nickname == Irc.Server.Username)
            {
                await Irc.AddChannel(channel);
            }

            if (parsedLine.CommandMessage.Parameters != null)
            {
                channel = parsedLine.CommandMessage.Parameters[0];
            }

            var msg = new Message
            {
                Type = MessageType.JoinPart,
                User = parsedLine.PrefixMessage.Nickname,
                Text = string.Format("({0}) {1}", parsedLine.PrefixMessage.Prefix, "joined the channel")
            };

            Irc.AddMessage(channel, msg);

            Irc.ChannelList[channel].Store.AddUser(parsedLine.PrefixMessage.Nickname, true);
            return true;
        }
    }
}