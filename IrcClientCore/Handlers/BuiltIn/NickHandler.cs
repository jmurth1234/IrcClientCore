using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class NickHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            if (Irc.ChannelList.ServerLog == null)
            {
                await Irc.AddChannel("Server");
            }

            if (parsedLine.PrefixMessage.IsUser && parsedLine.PrefixMessage.Nickname == Irc.Nickname) 
            {
                Irc.Server.Username = parsedLine.TrailMessage.HasTrail ? parsedLine.TrailMessage.TrailingContent : Irc.Nickname;
            }
            else if (parsedLine.PrefixMessage.IsUser && parsedLine.TrailMessage.HasTrail)
            {
                var username = parsedLine.PrefixMessage.Nickname;
                foreach (var channel in Irc.ChannelList)
                {
                    var users = channel.Store;
                    if (users.HasUser(username))
                    {
                        var msg = new Message
                        {
                            Type = MessageType.JoinPart,
                            User = parsedLine.PrefixMessage.Nickname,
                            Text = string.Format("({0}) {1} {2}", parsedLine.PrefixMessage.Prefix, "changed nick to", parsedLine.TrailMessage.TrailingContent)
                        };
                        Irc.AddMessage(channel.Name, msg);

                        users.RemoveUser(username);
                        users.AddUser(parsedLine.TrailMessage.TrailingContent, true);
                    }
                }
            }
            return true;
        }
    }
}
