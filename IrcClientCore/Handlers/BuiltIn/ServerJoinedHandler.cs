using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class ServerJoinedHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            if (Irc.Server.NickservPassword != null && Irc.Server.NickservPassword != "")
            {
                Irc.SendMessage("nickserv", "identify " + Irc.Server.NickservPassword);
            }

            if (Irc.Server.Channels != null && Irc.Server.Channels != "")
            {
                var channelsList = Irc.Server.Channels.Split(',');
                foreach (var channel in channelsList)
                {
                    await Irc.JoinChannel(channel);
                }
            }

            return true;
        }
    }
}
