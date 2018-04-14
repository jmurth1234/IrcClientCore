using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class DefaultHandler : BaseHandler
    {
        private bool passwordChanged;
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            if (Irc.ChannelList.ServerLog == null)
            {
                await Irc.AddChannel("Server");
            }

            if (!passwordChanged && parsedLine.CommandMessage.Parameters.Count == 1 && parsedLine.CommandMessage.Parameters[0] != Irc.Nickname) 
            {
                parsedLine.CommandMessage.Parameters[0] = parsedLine.PrefixMessage.Nickname;
                passwordChanged = true;
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
