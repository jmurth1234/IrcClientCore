using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class CannotSendHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var msg = new Message
            {
                Text = parsedLine.TrailMessage.TrailingContent,
                Type = MessageType.Info,
                Channel = parsedLine.CommandMessage.Parameters.Last(),
                User = ""
            };

            if (Irc.DebugMode) Debug.WriteLine(parsedLine.OriginalMessage);

            if (Irc.ChannelList.Contains(msg.Channel))
            {
                Irc.ChannelList[msg.Channel].Buffers.Add(msg);
            } 
            else
            {
                Irc.InfoBuffer.Add(msg);
            }

            return true;
        }
    }
}
