using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class QuitHandler : BaseHandler
    {
        public override Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var username = parsedLine.PrefixMessage.Nickname;
            foreach (var channel in Irc.ChannelList)
            {
                var users = channel.Store;
                if (users.HasUser(username))
                {
                    if ((!Config.Contains(Config.IgnoreJoinLeave)) || (!Config.GetBoolean(Config.IgnoreJoinLeave)))
                    {
                        Message msg = new Message();
                        msg.Type = MessageType.Info;
                        msg.User = parsedLine.PrefixMessage.Nickname;
                        msg.Text = String.Format("({0}) {1}: {2}", parsedLine.PrefixMessage.Prefix, "quit the server", parsedLine.TrailMessage.TrailingContent);
                        Irc.AddMessage(channel.Name, msg);
                    }

                    users.RemoveUser(username);
                }
            }

            return Task.FromResult(true);
        }
    }
}
