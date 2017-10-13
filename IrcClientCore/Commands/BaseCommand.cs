using System;
using System.Collections.Generic;
using System.Text;

namespace IrcClientCore.Commands
{
    public abstract class BaseCommand
    {
        public Irc Irc { get; internal set; }

        public abstract void RunCommand(string[] args);

        public virtual string[] GetCompletions(string word)
        {
            return new string[0];
        }

        protected void ClientMessage(string message)
        {
            if (Irc.CurrentChannel == null) return;
            Irc.ChannelList[Irc.CurrentChannel].ClientMessage(message);
        }
    }
}
