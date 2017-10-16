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
        private readonly string[] _whoisCmds = new string[] { "311", "319", "312", "330", "671", "317", "401" };

        public string Buffer;
        public string CurrentChannel { get; set; }
        public bool Transferred = false;

        internal bool IsReconnecting;

        public bool IsConnected = false;
        internal int ReconnectionAttempts;

        public bool ReadOrWriteFailed { get; internal set; }

        public Action<Irc> HandleDisconnect { get; set; }

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

        protected Irc(IrcServer server)
        {
            this.Server = server;
            ChannelList = new ChannelsGroup(new ObservableCollection<Channel>()) { Server = server.Name };

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
                await WriteLine(String.Format("NICK {0}", Server.Username));
                await WriteLine(String.Format("USER {0} {1} * :{2}", Server.Username, "8", Server.Username));
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

            if (parsedLine.CommandMessage.Command == "JOIN")
            {
                var channel = parsedLine.TrailMessage.TrailingContent;
                if (parsedLine.PrefixMessage.Nickname == this.Server.Username)
                {
                    AddChannel(channel);
                }

                if (parsedLine.CommandMessage.Parameters != null)
                {
                    channel = parsedLine.CommandMessage.Parameters[0];
                }

                if ((!Config.Contains(Config.IgnoreJoinLeave)) || (!Config.GetBoolean(Config.IgnoreJoinLeave)))
                {
                    Message msg = new Message();
                    msg.Type = MessageType.Info;
                    msg.User = parsedLine.PrefixMessage.Nickname;

                    msg.Text = String.Format("({0}) {1}", parsedLine.PrefixMessage.Prefix, "joined the channel");
                    AddMessage(channel, msg);
                }

                ChannelList[channel].Store.AddUser(parsedLine.PrefixMessage.Nickname, true);
            }
            else if (parsedLine.CommandMessage.Command == "PART")
            {
                var channel = parsedLine.TrailMessage.TrailingContent;
                if (parsedLine.PrefixMessage.Nickname == this.Server.Username)
                {
                    RemoveChannel(channel);
                }
                else
                {
                    if (parsedLine.CommandMessage.Parameters.Count > 0)
                    {
                        channel = parsedLine.CommandMessage.Parameters[0];
                    }

                    if ((!Config.Contains(Config.IgnoreJoinLeave)) || (!Config.GetBoolean(Config.IgnoreJoinLeave)))
                    {

                        Message msg = new Message();
                        msg.Type = MessageType.Info;
                        msg.User = parsedLine.PrefixMessage.Nickname;

                        msg.Text = String.Format("({0}) {1}", parsedLine.PrefixMessage.Prefix, "left the channel");
                        AddMessage(channel, msg);
                    }

                    ChannelList[channel].Store.RemoveUser(parsedLine.PrefixMessage.Nickname);
                }
            }
            else if (parsedLine.CommandMessage.Command == "PRIVMSG")
            {
                // handle messages to this irc client
                var destination = parsedLine.CommandMessage.Parameters[0];
                var content = parsedLine.TrailMessage.TrailingContent;

                if (destination == Server.Username) 
                {
                    destination = parsedLine.PrefixMessage.Nickname;
                }

                if (!ChannelList.Contains(destination))
                {
                    AddChannel(destination);
                }

                Message msg = new Message();

                msg.Type = MessageType.Normal;
                msg.User = parsedLine.PrefixMessage.Nickname;
                if (parsedLine.ServerTime != null)
                {
                    var time = DateTime.Parse(parsedLine.ServerTime);
                    msg.Timestamp = time.ToString("HH:mm");
                }

                if (content.Contains("ACTION"))
                {
                    msg.Text = content.Replace("ACTION ", "");
                    msg.Type = MessageType.Action;
                }
                else
                {
                    msg.Text = content;
                }

                if ((parsedLine.TrailMessage.TrailingContent.Contains(Server.Username) || parsedLine.CommandMessage.Parameters[0] == Server.Username))
                {
                    msg.Mention = true;
                }

                AddMessage(destination, msg);

            }
            else if (parsedLine.CommandMessage.Command == "KICK")
            {
                // handle messages to this irc client
                var destination = parsedLine.CommandMessage.Parameters[0];
                var reciever = parsedLine.CommandMessage.Parameters[1];
                var content = parsedLine.TrailMessage.TrailingContent;
                if (!ChannelList.Contains(destination))
                {
                    AddChannel(destination);
                }

                Message msg = new Message();

                msg.Type = MessageType.Info;

                if (reciever == Server.Username)
                {
                    msg.User = parsedLine.PrefixMessage.Nickname;
                    msg.Text = "kicked you from the channel: " + content;
                }
                else
                {
                    msg.User = parsedLine.PrefixMessage.Nickname;
                    msg.Text = String.Format("kicked {0} from the channel: {1}", reciever, content);
                }

                AddMessage(destination, msg);
            }
            else if (parsedLine.CommandMessage.Command == "353")
            {
                // handle /NAMES
                var list = parsedLine.TrailMessage.TrailingContent.Split(' ').ToList();
                var channel = parsedLine.CommandMessage.Parameters[2];

                if (!ChannelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                ChannelList[channel].Store.AddUsers(list);

                if (!Bouncer)
                {
                    ChannelList[channel].Store.SortUsers();
                }
            }
            else if (parsedLine.CommandMessage.Command == "332")
            {
                // handle initial topic recieve
                var topic = parsedLine.TrailMessage.TrailingContent;
                var channel = parsedLine.CommandMessage.Parameters[1];

                if (!ChannelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                Message msg = new Message();
                msg.Type = MessageType.Info;

                msg.User = "";
                msg.Text = String.Format("Topic for channel {0}: {1}", channel, topic);
                AddMessage(channel, msg);
                ChannelList[channel].Store.SetTopic(topic);
            }
            else if (parsedLine.CommandMessage.Command == "TOPIC")
            {
                // handle topic recieved
                var topic = parsedLine.TrailMessage.TrailingContent;
                var channel = parsedLine.CommandMessage.Parameters[0];

                if (!ChannelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                Message msg = new Message();
                msg.Type = MessageType.Info;

                msg.User = "";
                msg.Text = String.Format("Topic for channel {0}: {1}", channel, topic);
                AddMessage(channel, msg);
                ChannelList[channel].Store.SetTopic(topic);
            }
            else if (parsedLine.CommandMessage.Command == "QUIT")
            {
                var username = parsedLine.PrefixMessage.Nickname;
                foreach (var channel in ChannelList)
                {
                    var users = ChannelList[channel.Name].Store;
                    if (users.HasUser(username))
                    {
                        if ((!Config.Contains(Config.IgnoreJoinLeave)) || (!Config.GetBoolean(Config.IgnoreJoinLeave)))
                        {
                            Message msg = new Message();
                            msg.Type = MessageType.Info;
                            msg.User = parsedLine.PrefixMessage.Nickname;
                            msg.Text = String.Format("({0}) {1}: {2}", parsedLine.PrefixMessage.Prefix, "quit the server", parsedLine.TrailMessage.TrailingContent);
                            AddMessage(channel.Name, msg);
                        }

                        users.RemoveUser(username);
                    }
                }
            }
            else if (parsedLine.CommandMessage.Command == "MODE")
            {
                Console.WriteLine(parsedLine.CommandMessage.Command + " - " + receivedData);

                if (parsedLine.CommandMessage.Parameters.Count > 2)
                {
                    var channel = parsedLine.CommandMessage.Parameters[0];

                    if (parsedLine.CommandMessage.Parameters.Count == 3)
                    {
                        string currentPrefix = ChannelList[channel].Store.GetPrefix(parsedLine.CommandMessage.Parameters[2]);
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

                        ChannelList[channel].Store.ChangePrefix(parsedLine.CommandMessage.Parameters[2], prefix);
                    }

                    ClientMessage(channel, "Mode change: " + String.Join(" ", parsedLine.CommandMessage.Parameters));
                }
            }
            else if (parsedLine.CommandMessage.Command == "470")
            {
                RemoveChannel(parsedLine.CommandMessage.Parameters[1]);
                AddChannel(parsedLine.CommandMessage.Parameters[2]);
            }
            else if (_whoisCmds.Any(str => str.Contains(parsedLine.CommandMessage.Command)))
            {
                var cmd = parsedLine.CommandMessage.Command;
                if (_currentWhois == "")
                {
                    _currentWhois += "Whois for " + parsedLine.CommandMessage.Parameters[1] + ": \r\n";
                }

                var whoisLine = "";

                if (cmd == "330")
                {
                    whoisLine += parsedLine.CommandMessage.Parameters[1] + " " + parsedLine.TrailMessage.TrailingContent + " " + parsedLine.CommandMessage.Parameters[2] + " ";
                    _currentWhois += whoisLine + "\r\n";

                }
                else
                {
                    for (int i = 2; i < parsedLine.CommandMessage.Parameters.Count; i++)
                    {
                        whoisLine += parsedLine.CommandMessage.Command + " " + parsedLine.CommandMessage.Parameters[i] + " ";
                    }
                    _currentWhois += whoisLine + parsedLine.TrailMessage.TrailingContent + "\r\n";

                }

            }
            else if (parsedLine.CommandMessage.Command == "318")
            {
                Console.WriteLine(_currentWhois);
                Message msg = new Message();
                msg.Text = _currentWhois;
                msg.Type = MessageType.Info;
                AddMessage(CurrentChannel, msg);

                _currentWhois = "";
            }
            else if (parsedLine.CommandMessage.Command == "376")
            {
                if (Server.NickservPassword != null && Server.NickservPassword != "")
                {
                    SendMessage("nickserv", "identify " + Server.NickservPassword);
                }

                if (Server.Channels != null && Server.Channels != "")
                {
                    var channelsList = Server.Channels.Split(',');
                    foreach (string channel in channelsList)
                    {
                        JoinChannel(channel);
                    }
                }
            }
        }


        public void SendAction(string message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.Type = MessageType.Action;
            msg.User = Server.Username;
            AddMessage(CurrentChannel, msg);

            WriteLine(String.Format("PRIVMSG {0} :\u0001ACTION {1}\u0001", CurrentChannel, message));
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

        public void SendMessage(string message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.User = Server.Username;
            msg.Type = MessageType.Normal;

            AddMessage(CurrentChannel, msg);

            WriteLine(String.Format("PRIVMSG {0} :{1}", CurrentChannel, message));
        }

        public async void AddError(String message)
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

                if (CurrentChannel == channel)
                {
                    CurrentChannel = "";
                }
            }
        }

        public void SwitchChannel(string channel)
        {
            if (channel == null)
            {
                return;
            }

            if (ChannelList.Contains(channel))
            {
                CurrentChannel = channel;
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

        public abstract Task WriteLine(string str);
        
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

    }
}
