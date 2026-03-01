using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class MotdHandler : BaseHandler
    {
        private string _currentMOTD;

        public override Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var cmd = parsedLine.CommandMessage.Command;
            if (cmd == "375")
            {
                _currentMOTD = parsedLine.TrailMessage.TrailingContent + "\r\n";
            }

            if (cmd == "372")
            {
                _currentMOTD += parsedLine.TrailMessage.TrailingContent + "\r\n";
            }

            if (cmd == "376" || cmd == "422")
            {
                _currentMOTD += parsedLine.TrailMessage.TrailingContent;

                var formatted = IrcFormatParser.Parse(_currentMOTD);
                var msg = new Message
                {
                    Text = formatted.PlainText,
                    FormattedText = formatted,
                    Type = MessageType.MOTD
                };
                Irc.AddMessage("", msg);

                Irc.MOTD = _currentMOTD;
                _currentMOTD = "";
            }

            return Task.FromResult(true);
        }
    }
}

