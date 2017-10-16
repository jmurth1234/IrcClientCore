using System;
using System.Collections.Generic;
using System.Text;

namespace IrcClientCore.Handlers.BuiltIn
{
    class CapHandler : BaseHandler
    {
        public override void HandleLine(IrcMessage parsedLine)
        {
            if (parsedLine.CommandMessage.Parameters[1] == "LS")
            {
                var requirements = "";
                var compatibleFeatues = parsedLine.TrailMessage.TrailingContent;

                if (compatibleFeatues.Contains("znc"))
                {
                    Irc.Bouncer = true;
                }

                if (compatibleFeatues.Contains("znc.in/server-time-iso"))
                {
                    requirements += "znc.in/server-time-iso ";
                }

                if (compatibleFeatues.Contains("multi-prefix"))
                {
                    requirements += "multi-prefix ";
                }

                Irc.WriteLine("CAP REQ :" + requirements);
                Irc.WriteLine("CAP END");
            }

        }
    }
}
