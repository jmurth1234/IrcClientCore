using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class DefaultHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            if (Irc.ChannelList.ServerLog == null)
            {
                await Irc.AddChannel("Server");
            }

            Message msg = new Message
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
