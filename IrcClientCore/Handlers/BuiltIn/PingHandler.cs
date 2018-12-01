using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class PingHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var pong = parsedLine.OriginalMessage.Replace("PING", "PONG");
            Irc.WriteLine(pong);
            return true;
        }
    }
}
