using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class WhoisHandler : BaseHandler
    {
        private string _currentWhois;

        public override Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var cmd = parsedLine.CommandMessage.Command;
            if (_currentWhois == "")
            {
                _currentWhois += "Whois for " + parsedLine.CommandMessage.Parameters[1] + ": \r\n";
            }

            var whoisLine = "";

            if (cmd == "330")
            {
                whoisLine += parsedLine.CommandMessage.Parameters[1] + " " + parsedLine.TrailMessage.TrailingContent + " " + parsedLine.CommandMessage.Parameters[2] + " ";
                _currentWhois += whoisLine + "\r\n";
            }
            else
            {
                for (int i = 2; i < parsedLine.CommandMessage.Parameters.Count; i++)
                {
                    whoisLine += parsedLine.CommandMessage.Command + " " + parsedLine.CommandMessage.Parameters[i] + " ";
                }
                _currentWhois += whoisLine + parsedLine.TrailMessage.TrailingContent + "\r\n";

            }

            if (cmd == "318")
            {
                Message msg = new Message();
                msg.Text = _currentWhois;
                msg.Type = MessageType.Info;
                Irc.AddMessage(Irc.WhoisDestination, msg);

                _currentWhois = "";
                Irc.WhoisDestination = "";
            }

            return Task.FromResult(true);
        }
    }
}
