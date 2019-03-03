using IrcClientCore.Handlers.BuiltIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IrcClientCore.Handlers
{
    public class HandlerManager
    {
        private ICollection<BaseHandler> Handlers = new List<BaseHandler>();

        private Irc Server { get; set; }

        private BaseHandler DefaultHandler { get; set; }

        private readonly string[] _whoisCmds = new string[] { "311", "319", "318", "312", "330", "671", "317", "401" };
        private readonly string[] _topicCmds = new string[] { "TOPIC", "332" };


        public HandlerManager(Irc irc)
        {
            this.Server = irc;
            this.DefaultHandler = new DefaultHandler();
            this.DefaultHandler.Irc = irc;

            // register the default handlers
            RegisterHandler("ING", new PingHandler());
            RegisterHandler("CAP", new CapHandler());
            RegisterHandler("NICK", new NickHandler());
            RegisterHandler("PRIVMSG", new PrivmsgHandler());
            RegisterHandler("JOIN", new JoinHandler());
            RegisterHandler("PART", new PartHandler());
            RegisterHandler("KICK", new KickHandler());
            RegisterHandler("353", new NamesHandler());
            RegisterHandler("QUIT", new QuitHandler());
            RegisterHandler("MODE", new ModeHandler());
            RegisterHandler("376", new ServerJoinedHandler());
            RegisterHandler("470", new ChannelForwardHandler());
            MultiRegisterHandler(_whoisCmds, new WhoisHandler());
            MultiRegisterHandler(_topicCmds, new TopicHandler());
        }

        private void MultiRegisterHandler(string[] commands, BaseHandler handler, HandlerPriority priority = HandlerPriority.MEDIUM)
        {
            handler.Irc = Server;
            handler.Priority = priority;
            foreach (var command in commands)
            {
                handler.Commands.Add(command);
            }
            Handlers.Add(handler);
        }

        private void RegisterHandler(string command, BaseHandler handler, HandlerPriority priority = HandlerPriority.MEDIUM)
        {
            handler.Irc = Server;
            handler.Priority = priority;
            handler.Commands.Add(command);
            Handlers.Add(handler);
        }

        internal List<BaseHandler> GetHandlers(string potentialCommand)
        {
            var handlers = Handlers.Where(handler => handler.Commands.Contains(potentialCommand))
                                   .OrderByDescending(handler => (int) handler.Priority).ToList();

            if (handlers.Count >= 1)
            {
                return handlers;
            }

            return new List<BaseHandler>() { DefaultHandler };
        }

    }
}