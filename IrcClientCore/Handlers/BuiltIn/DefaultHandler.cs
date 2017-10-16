using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace IrcClientCore.Handlers.BuiltIn
{
    class DefaultHandler : BaseHandler
    {
        public override async void HandleLine(IrcMessage parsedLine)
        {
            if (Irc.ChannelList.ServerLog == null)
            {
                await Irc.AddChannel("Server");
            }

            Message msg = new Message();
            msg.Text = parsedLine.OriginalMessage;
            msg.Type = MessageType.Info;
            msg.User = "";
            Debug.WriteLine(parsedLine.OriginalMessage);
            Irc.ChannelList.ServerLog?.Buffers.Add(msg);
        }
    }
}
