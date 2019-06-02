using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class CapHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
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

                if (compatibleFeatues.Contains("echo-message"))
                {
                    requirements += "echo-message ";
                    Irc.SupportsSelfMsg = true;
                }

                if (compatibleFeatues.Contains("znc.in/self-message"))
                {
                    requirements += "znc.in/self-message ";
                    Irc.SupportsSelfMsg = true;
                }

                Irc.WriteLine("CAP REQ :" + requirements);
                Irc.WriteLine("CAP END");
            }

            return true;
        }
    }
}
