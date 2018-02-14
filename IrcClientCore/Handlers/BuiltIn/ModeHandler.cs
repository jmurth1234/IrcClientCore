using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class ModeHandler : BaseHandler
    {
        public override Task<bool> HandleLine(IrcMessage parsedLine)
        {
            Debug.WriteLine(parsedLine.CommandMessage.Command + " - " + parsedLine.OriginalMessage);

            if (parsedLine.CommandMessage.Parameters.Count > 2)
            {
                var channel = parsedLine.CommandMessage.Parameters[0];

                if (parsedLine.CommandMessage.Parameters.Count == 3)
                {
                    string currentPrefix = Irc.ChannelList[channel].Store.GetPrefix(parsedLine.CommandMessage.Parameters[2]);
                    string prefix = "";
                    string mode = parsedLine.CommandMessage.Parameters[1];
                    if (mode == "+o")
                    {
                        if (currentPrefix.Length > 0 && currentPrefix[0] == '+')
                        {
                            prefix = "@+";
                        }
                        else
                        {
                            prefix = "@";
                        }
                    }
                    else if (mode == "-o")
                    {
                        if (currentPrefix.Length > 0 && currentPrefix[1] == '+')
                        {
                            prefix = "+";
                        }
                    }
                    else if (mode == "+v")
                    {
                        if (currentPrefix.Length > 0 && currentPrefix[0] == '@')
                        {
                            prefix = "@+";
                        }
                        else
                        {
                            prefix = "+";
                        }
                    }
                    else if (mode == "-v")
                    {
                        if (currentPrefix.Length > 0 && currentPrefix[0] == '@')
                        {
                            prefix = "@";
                        }
                        else
                        {
                            prefix = "";
                        }
                    }

                    Irc.ChannelList[channel].Store.ChangePrefix(parsedLine.CommandMessage.Parameters[2], prefix);
                }

                Irc.ClientMessage(channel, "Mode change: " + String.Join(" ", parsedLine.CommandMessage.Parameters));
            }

            return Task.FromResult(true);
        }
    }
}
