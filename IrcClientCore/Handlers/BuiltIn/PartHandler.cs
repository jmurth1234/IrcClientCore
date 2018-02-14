using System;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    public class PartHandler : BaseHandler
    {
        public override Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var channel = parsedLine.TrailMessage.TrailingContent;
            if (parsedLine.PrefixMessage.Nickname == Irc.Server.Username)
            {
                Irc.RemoveChannel(channel);
            }
            else
            {
                if (parsedLine.CommandMessage.Parameters.Count > 0)
                {
                    channel = parsedLine.CommandMessage.Parameters[0];
                }

                if ((!Config.Contains(Config.IgnoreJoinLeave)) || (!Config.GetBoolean(Config.IgnoreJoinLeave)))
                {

                    Message msg = new Message();
                    msg.Type = MessageType.Info;
                    msg.User = parsedLine.PrefixMessage.Nickname;

                    msg.Text = String.Format("({0}) {1}", parsedLine.PrefixMessage.Prefix, "left the channel");
                    Irc.AddMessage(channel, msg);
                }

                Irc.ChannelList[channel].Store.RemoveUser(parsedLine.PrefixMessage.Nickname);
            }
            return Task.FromResult(true);
        }
    }
}