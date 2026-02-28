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
        private readonly string[] _namesCmds = new string[] { "353", "366" };
        private readonly string[] _listCmds = new string[] { "321", "322", "323" };
        private readonly string[] _motdCmds = new string[] { "375", "372", "376", "422" };
        private readonly string[] _endMotd = new string[] { "376", "422" };
        private readonly string[] _cannotSend = new string[] { "401", "403", "404" };
        private readonly string[] _nickErrors = new string[] { "433", "436" };

        public HandlerManager(Irc irc)
        {
            this.Server = irc;
            this.DefaultHandler = new DefaultHandler();
            this.DefaultHandler.Irc = irc;

            // register the default handlers
            RegisterHandler("ING", new PingHandler());
            RegisterHandler("CAP", new CapHandler());
            RegisterHandler("AUTHENTICATE", new AuthenticateHandler());
            RegisterHandler("903", new AuthenticateHandler()); // SASL success
            RegisterHandler("904", new AuthenticateHandler()); // SASL failure
            RegisterHandler("905", new AuthenticateHandler()); // SASL failure
            RegisterHandler("906", new AuthenticateHandler()); // SASL aborted
            RegisterHandler("907", new AuthenticateHandler()); // Already authenticated
            RegisterHandler("NICK", new NickHandler());
            RegisterHandler("PRIVMSG", new PrivmsgHandler());
            RegisterHandler("NOTICE", new PrivmsgHandler() { Type = MessageType.Notice });
            RegisterHandler("JOIN", new JoinHandler());
            RegisterHandler("PART", new PartHandler());
            RegisterHandler("KICK", new KickHandler());
            RegisterHandler("QUIT", new QuitHandler());
            RegisterHandler("MODE", new ModeHandler());
            RegisterHandler("470", new ChannelForwardHandler());
            RegisterHandler("421", new UnknownCommandHandler());
            MultiRegisterHandler(_whoisCmds, new WhoisHandler());
            MultiRegisterHandler(_topicCmds, new TopicHandler());
            MultiRegisterHandler(_listCmds, new ListHandler());
            MultiRegisterHandler(_namesCmds, new NamesHandler());
            MultiRegisterHandler(_motdCmds, new MotdHandler());
            MultiRegisterHandler(_endMotd, new ServerJoinedHandler());
            MultiRegisterHandler(_cannotSend, new CannotSendHandler());
            MultiRegisterHandler(_nickErrors, new NickErrorHandler());

            // IRCv3.2 away-notify
            MultiRegisterHandler(new string[] { "301", "305", "306", "AWAY" }, new AwayHandler());

            // IRCv3.2 monitor
            var monitorHandler = new MonitorHandler();
            RegisterHandler("MONITOR", monitorHandler);
            MultiRegisterHandler(new string[] { "730", "731", "732", "733", "734" }, new MonitorHandler());

            // IRCv3.2 batch
            RegisterHandler("BATCH", new BatchHandler());

            // IRCv3.2 chghost
            RegisterHandler("CHGHOST", new ChgHostHandler());

            // IRCv3.2 setname
            RegisterHandler("SETNAME", new SetNameHandler());

            // IRCv3.2 account-notify
            RegisterHandler("ACCOUNT", new AccountNotifyHandler());

            // IRCv3 TAGMSG (typing indicators, reactions)
            RegisterHandler("TAGMSG", new TagMsgHandler());
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

        public bool HasHandler(string command)
        {
            return Handlers
                    .Where(handler => handler.Commands.Contains(command))
                    .OrderByDescending(handler => (int)handler.Priority).ToList().Count > 0;
        }

        public T GetHandler<T>() where T : BaseHandler
        {
            return Handlers.OfType<T>().FirstOrDefault();
        }

    }
}