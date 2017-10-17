using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers
{
    public abstract class BaseHandler
    {
        public Irc Irc { get; internal set; }
        public HandlerPriority Priority { get; internal set; }
        public HashSet<string> Commands { get; internal set; } = new HashSet<string>();

        // return true for other handlers to handle this command, false to cancel further processing
        public abstract Task<bool> HandleLine(IrcMessage parsedLine);
    }

    public enum HandlerPriority { HIGH = 2, MEDIUM = 1, LOW = 0 }
}
