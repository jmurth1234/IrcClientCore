using IrcClientCore.Commands;
using IrcClientCore.Handlers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public abstract class Irc
    {
        public IrcServer Server { get; set; }

        public bool IsAuthed { get; set; }

        public ChannelsGroup ChannelList { get; set; }

        public CommandManager CommandManager { get; private set; }
        internal HandlerManager HandlerManager { get; }

        private string _currentWhois = "";

        public string Buffer;
        public bool Transferred = false;

        internal bool IsReconnecting;

        public bool IsConnected = false;
        internal int ReconnectionAttempts;

        public bool ReadOrWriteFailed { get; internal set; }

        public Action<Irc> HandleDisconnect { get; set; }
        public ObservableCollection<Message> Mentions { get; set; }

        public string Nickname {
            get => Server.Username;
            set
            {
                Server.Username = value;
                WriteLine("NICK " + value);

                foreach (Channel channel in ChannelList)
                {
                    channel.ClientMessage("Changed username to " + value);
                }
            }
        }

        public bool Bouncer { get; internal set; }
        internal string WhoisDestination { get; set; }

        protected Irc(IrcServer server)
        {
            this.Server = server;
            ChannelList = new ChannelsGroup(new ObservableCollection<Channel>()) { Server = server.Name };
            Mentions = new ObservableCollection<Message>();
            this.CommandManager = new CommandManager(this);
            this.HandlerManager = new HandlerManager(this);

            IsAuthed = false;

            AddChannel("Server");
        }

        private void ConnectionChanged(bool connected)
        {
            if (connected && Config.GetBoolean(Config.AutoReconnect))
            {
                foreach (Channel channel in ChannelList)
                {
                    channel.ClientMessage("Reconnecting...");
                }
                Connect();
            }
            else
            {
                foreach (Channel channel in ChannelList)
                {
                    channel.ClientMessage("Disconnected from IRC");
                }
                DisconnectAsync(attemptReconnect: true);
            }
        }

        public virtual async void Connect() { }
        public virtual async void DisconnectAsync(string msg = "Powered by WinIRC", bool attemptReconnect = false) { }
        public virtual async void SocketTransfer() { }
        public virtual async void SocketReturn() { }

        internal void AttemptAuth()
        {
            // Auth to the server
            Console.WriteLine("Attempting to auth");

            WriteLine("CAP LS");

            AttemptRegister();

            if (Server.Password != "")
            {
                WriteLine("PASS " + Server.Password);
            }

            IsAuthed = true;
        }

        private async void AttemptRegister()
        {
            try
            {
                WriteLine(String.Format("NICK {0}", Server.Username));
                WriteLine(String.Format("USER {0} {1} * :{2}", Server.Username, "8", Server.Username));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        internal async Task HandleLine(string receivedData)
        {
            if (receivedData.Contains("Nickname is already in use"))
            {
                this.Server.Username += "_";
                AttemptAuth();
                return;
            }

            if (receivedData.StartsWith("ERROR"))
            {
                if (!IsReconnecting)
                {
                    ReadOrWriteFailed = true;

                    var autoReconnect = Config.GetBoolean(Config.AutoReconnect);

                    var msg = autoReconnect
                        ? "Attempting to reconnect..."
                        : "Please try again later.";

                    AddError("Error with connection: \n" + msg);

                    DisconnectAsync(attemptReconnect: autoReconnect);
                }
                return;
            }

            if (receivedData.StartsWith("PING"))
            {
                WriteLine(receivedData.Replace("PING", "PONG"));
                return;
            }

            var parsedLine = new IrcMessage(receivedData);

            ReconnectionAttempts = 0;

            var handlers = HandlerManager.GetHandlers(parsedLine.CommandMessage.Command);
            foreach (var handler in handlers)
            {
                var cont = await handler.HandleLine(parsedLine);
                if (!cont) break;
            }
        }


        public void SendAction(string channel, string message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.Type = MessageType.Action;
            msg.User = Server.Username;
            AddMessage(channel, msg);

            WriteLine(String.Format("PRIVMSG {0} :\u0001ACTION {1}\u0001", channel, message));
        }

        public void SendMessage(string channel, string message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.User = Server.Username;
            msg.Type = MessageType.Normal;

            if (ChannelList.Contains(channel))
                ChannelList[channel].Buffers.Add(msg);

            WriteLine(String.Format("PRIVMSG {0} :{1}", channel, message));
        }

        public string GetChannelTopic(string channel)
        {
            if (ChannelList.Contains(channel))
                return ChannelList[channel].Store.Topic;

            return "";
        }

        public void JoinChannel(string channel)
        {
            WriteLine(String.Format("JOIN {0}", channel));
        }

        public void PartChannel(string channel)
        {
            WriteLine(String.Format("PART {0}", channel));
            RemoveChannel(channel);
        }

        public void AddError(String message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.User = "Error";
            msg.Type = MessageType.Info;
            msg.Mention = true;

            AddMessage("Server", msg);
        }

        public async void AddMessage(string channel, Message msg)
        {
            if (Server == null)
            {
                return;
            }

            if (!ChannelList.Contains(channel))
            {
                await AddChannel(channel);
            }

            ChannelList[channel].Buffers.Add(msg);
        }

        public async Task<bool> AddChannel(string channel)
        {
            if (channel == "")
            {
                return false;
            }

            if (!ChannelList.Contains(channel))
            {
                var comparer = Comparer<String>.Default;

                int i = 0;

                while (i < ChannelList.Count && comparer.Compare(ChannelList[i].Name.ToLower(), channel.ToLower()) < 0)
                    i++;

                ChannelList.Insert(i, channel);
            }

            await Task.Delay(1);

            if (!Config.Contains(Config.SwitchOnJoin))
            {
                Config.SetBoolean(Config.SwitchOnJoin, true);
            }

            return ChannelList.Contains(channel);
        }

        public void RemoveChannel(string channel)
        {
            if (ChannelList.Contains(channel))
            {
                ChannelList.Remove(channel);
            }
        }
        
        public void ClientMessage(string channel, string text)
        {
            Message msg = new Message();
            msg.User = "";
            msg.Type = MessageType.Info;
            msg.Text = text;

            this.AddMessage(channel, msg);
        }

        public abstract void WriteLine(string str);
        
        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public ObservableCollection<string> GetChannelUsers(string channel)
        {
            if (!ChannelList.Contains(channel))
            {
                return new ObservableCollection<string>();
            }

            var store = ChannelList[channel].Store;

            if (store.SortedUsers.Count == 0)
                store.SortUsers();

            return store.SortedUsers;
        }

        public ObservableCollection<string> GetRawUsers(string channel)
        {
            if (!ChannelList.Contains(channel))
            {
                return new ObservableCollection<string>();
            }

            return  ChannelList[channel].Store.RawUsers;
        }

        public void AddMention(Message message)
        {
            Mentions.Add(message);
        }
    }
}
