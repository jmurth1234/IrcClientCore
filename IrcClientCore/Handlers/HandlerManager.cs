using IrcClientCore.Handlers.BuiltIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IrcClientCore.Handlers
{
    class HandlerManager
    {
        private ICollection<BaseHandler> Handlers = new List<BaseHandler>();

        private Irc Server { get; set; }

        private BaseHandler DefaultHandler { get; set; }

        public HandlerManager(Irc irc)
        {
            this.Server = irc;
            this.DefaultHandler = new DefaultHandler();
            this.DefaultHandler.Irc = irc;

            RegisterHandler("CAP", new CapHandler());
        }

        private void RegisterHandler(string command, BaseHandler handler)
        {
            RegisterHandler(command, handler, HandlerPriority.MEDIUM);
        }

        private void RegisterHandler(string command, BaseHandler handler, HandlerPriority priority)
        {
            handler.Irc = Server;
            handler.Priority = priority;
            handler.Command = command;
            Handlers.Add(handler);
        }

        internal List<BaseHandler> GetHandlers(string potentialCommand)
        {
            var handlers = Handlers.Where(handler => handler.Command.Equals(potentialCommand))
                                   .OrderByDescending(handler => (int) handler.Priority).ToList();

            if (handlers.Count >= 1)
            {
                return handlers;
            }

            return new List<BaseHandler>() { DefaultHandler };
        }

    }
}