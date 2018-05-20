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
            
            var msg = new Message
            {
                Text = parsedLine.OriginalMessage,
                Type = MessageType.Info,
                User = ""
            };

            Debug.WriteLine(parsedLine.OriginalMessage);
            Irc.ChannelList.ServerLog?.Buffers.Add(msg);
            return true;
        }
    }
}
