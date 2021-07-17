using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class NickErrorHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var message = parsedLine.TrailMessage.TrailingContent;

            var msg = new Message
            {
                Text = message,
                Type = MessageType.Info,
                User = ""
            };

            if (Irc.DebugMode) Debug.WriteLine(parsedLine.OriginalMessage);

            Irc.InfoBuffer.Add(msg);

            Irc.Nickname = Irc.Nickname + "_";

            return true;
        }
    }
}
