using IrcClientCore.Handlers.BuiltIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IrcClientCore.Handlers
{
    class HandlerManager
    {
        private Dictionary<String, BaseHandler> _handlerTable = new Dictionary<String, BaseHandler>();
        private ICollection<string> HandlerCommands => _handlerTable.Keys;

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
            handler.Irc = Server;
            _handlerTable.Add(command, handler);
        }

        internal BaseHandler GetHandler(string potentialCommand)
        {
            var cmd = HandlerCommands.Where(command => command.StartsWith(potentialCommand)).ToList();
            var channel = Server.ChannelList[Server.CurrentChannel];

            if (cmd.Count == 1)
            {
                return _handlerTable[cmd[0]];
            }

            return DefaultHandler;
        }

    }
}