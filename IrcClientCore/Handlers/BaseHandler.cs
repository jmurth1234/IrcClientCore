using System;
using System.Collections.Generic;
using System.Text;

namespace IrcClientCore.Handlers
{
    public abstract class BaseHandler
    {
        public Irc Irc { get; internal set; }

        public abstract void HandleLine(IrcMessage parsedLine);
     }
}
