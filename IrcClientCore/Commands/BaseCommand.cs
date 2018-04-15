using System;
using System.Collections.Generic;
using System.Text;

namespace IrcClientCore.Commands
{
    public abstract class BaseCommand
    {
        public Irc Irc { get; internal set; }

        public abstract void RunCommand(string channel, string[] args);

        public virtual string[] GetCompletions(string channel, string word)
        {
            return new string[0];
        }

        protected void ClientMessage(string channel, string message)
        {
            if (channel == null) return;
            Irc.ChannelList[channel].ClientMessage(message);
        }
    }
}
