using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class UnknownCommandHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var command = parsedLine.CommandMessage.Parameters.Last();
            var msg = new Message
            {
                Text = $"{command}: {parsedLine.TrailMessage.TrailingContent}",
                Type = MessageType.Info,
                User = ""
            };

            if (Irc.DebugMode) Debug.WriteLine(parsedLine.OriginalMessage);

            Irc.InfoBuffer.Add(msg);

            return true;
        }
    }
}
