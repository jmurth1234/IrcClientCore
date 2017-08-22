using IrcClientCore.Commands;
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
        public IrcServer server { get; set; }

        public String BackgroundTaskName {
            get
            {
                return "WinIRCBackgroundTask." + server.name;
            }
        }

        public bool IsAuthed { get; set; }

        public ObservableCollection<Message> ircMessages { get; set; }

        public ServerGroup channelList { get; set; }

        public CommandManager CommandManager { get; private set; }

        public Dictionary<string, ChannelStore> channelStore { get; set; }

        private bool autoReconnect = true;

        public Dictionary<string, ObservableCollection<Message>> channelBuffers { get; set; }

        private string currentWhois = "";
        private string[] WhoisCmds = new string[] { "311", "319", "312", "330", "671", "317", "401" };

        public string buffer;
        public string currentChannel;
        public bool Transferred = false;

        private string lightTextColor;
        private string chatTextColor;

        internal bool IsReconnecting;

        public bool IsConnected = false;
        internal int ReconnectionAttempts;
        internal bool ReadOrWriteFailed;

        public Action<Irc> HandleDisconnect { get; set; }

        public string Nickname {
            get
            {
                return server.username;
            }
            set
            {
                server.username = value;
                WriteLine("NICK " + value);

                foreach (string channel in channelBuffers.Keys)
                {
                    ClientMessage(channel, "Changed username to " + value);
                }
            }
        }

        public bool IsBouncer { get; private set; }

        public Irc(IrcServer server)
        {
            this.server = server;
            ircMessages = new ObservableCollection<Message>();

            channelList = new ServerGroup(new ObservableCollection<Channel>());
            channelList.Server = server.name;

            channelBuffers = new Dictionary<string, ObservableCollection<Message>>(StringComparer.OrdinalIgnoreCase);
            channelStore = new Dictionary<string, ChannelStore>(StringComparer.OrdinalIgnoreCase);

            this.CommandManager = new CommandManager(this);

            IsAuthed = false;

            AddChannel("Server");
        }

        private void ConnectionChanged(bool connected)
        {
            if (connected && Config.GetBoolean(Config.AutoReconnect))
            {
                foreach (string channel in channelBuffers.Keys)
                {
                    ClientMessage(channel, "Reconnecting...");
                }
                Connect();
            }
            else
            {
                foreach (string channel in channelBuffers.Keys)
                {
                    ClientMessage(channel, "Disconnected from IRC");
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

            if (server.password != "")
            {
                WriteLine("PASS " + server.password);
            }

            IsAuthed = true;
        }

        private void AttemptRegister()
        {
            try
            {
                WriteLine(String.Format("NICK {0}", server.username));
                WriteLine(String.Format("USER {0} {1} * :{2}", server.username, "8", server.username));
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
                this.server.username += "_";
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
                await WriteLine(receivedData.Replace("PING", "PONG"));
                return;
            }

            var parsedLine = new IrcMessage(receivedData);

            ReconnectionAttempts = 0;

            if (parsedLine.CommandMessage.Command == "CAP")
            {
                if (parsedLine.CommandMessage.Parameters[1] == "LS")
                {
                    var requirements = "";
                    var compatibleFeatues = parsedLine.TrailMessage.TrailingContent;

                    if (compatibleFeatues.Contains("znc"))
                    {
                        IsBouncer = true;
                    }

                    if (compatibleFeatues.Contains("znc.in/server-time-iso"))
                    {
                        requirements += "znc.in/server-time-iso ";
                    }

                    if (compatibleFeatues.Contains("multi-prefix"))
                    {
                        requirements += "multi-prefix ";
                    }


                    WriteLine("CAP REQ :" + requirements);
                    WriteLine("CAP END");
                }
            }
            else if (parsedLine.CommandMessage.Command == "JOIN")
            {
                var channel = parsedLine.TrailMessage.TrailingContent;
                if (parsedLine.PrefixMessage.Nickname == this.server.username)
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

                channelStore[channel].AddUser(parsedLine.PrefixMessage.Nickname, true);
            }
            else if (parsedLine.CommandMessage.Command == "PART")
            {
                var channel = parsedLine.TrailMessage.TrailingContent;
                if (parsedLine.PrefixMessage.Nickname == this.server.username)
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

                    channelStore[channel].RemoveUser(parsedLine.PrefixMessage.Nickname);
                }
            }
            else if (parsedLine.CommandMessage.Command == "PRIVMSG")
            {
                // handle messages to this irc client
                var destination = parsedLine.CommandMessage.Parameters[0];
                var content = parsedLine.TrailMessage.TrailingContent;

                if (destination == server.username) 
                {
                    destination = parsedLine.PrefixMessage.Nickname;
                }

                if (!channelList.Contains(destination))
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

                if ((parsedLine.TrailMessage.TrailingContent.Contains(server.username) || parsedLine.CommandMessage.Parameters[0] == server.username))
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
                if (!channelList.Contains(destination))
                {
                    AddChannel(destination);
                }

                Message msg = new Message();

                msg.Type = MessageType.Info;

                if (reciever == server.username)
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

                if (!channelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                channelStore[channel].AddUsers(list);

                if (!IsBouncer)
                {
                    channelStore[channel].SortUsers();
                }
            }
            else if (parsedLine.CommandMessage.Command == "332")
            {
                // handle initial topic recieve
                var topic = parsedLine.TrailMessage.TrailingContent;
                var channel = parsedLine.CommandMessage.Parameters[1];

                if (!channelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                Message msg = new Message();
                msg.Type = MessageType.Info;

                msg.User = "";
                msg.Text = String.Format("Topic for channel {0}: {1}", channel, topic);
                AddMessage(channel, msg);
                channelStore[channel].SetTopic(topic);
            }
            else if (parsedLine.CommandMessage.Command == "TOPIC")
            {
                // handle topic recieved
                var topic = parsedLine.TrailMessage.TrailingContent;
                var channel = parsedLine.CommandMessage.Parameters[0];

                if (!channelList.Contains(channel))
                {
                    await AddChannel(channel);
                }

                Message msg = new Message();
                msg.Type = MessageType.Info;

                msg.User = "";
                msg.Text = String.Format("Topic for channel {0}: {1}", channel, topic);
                AddMessage(channel, msg);
                channelStore[channel].SetTopic(topic);
            }
            else if (parsedLine.CommandMessage.Command == "QUIT")
            {
                var username = parsedLine.PrefixMessage.Nickname;
                foreach (var channel in channelList)
                {
                    var users = channelStore[channel.Name];
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
                        string currentPrefix = channelStore[channel].GetPrefix(parsedLine.CommandMessage.Parameters[2]);
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

                        channelStore[channel].ChangePrefix(parsedLine.CommandMessage.Parameters[2], prefix);
                    }

                    ClientMessage(channel, "Mode change: " + String.Join(" ", parsedLine.CommandMessage.Parameters));
                }
            }
            else if (parsedLine.CommandMessage.Command == "470")
            {
                RemoveChannel(parsedLine.CommandMessage.Parameters[1]);
                AddChannel(parsedLine.CommandMessage.Parameters[2]);
            }
            else if (WhoisCmds.Any(str => str.Contains(parsedLine.CommandMessage.Command)))
            {
                var cmd = parsedLine.CommandMessage.Command;
                if (currentWhois == "")
                {
                    currentWhois += "Whois for " + parsedLine.CommandMessage.Parameters[1] + ": \r\n";
                }

                var whoisLine = "";

                if (cmd == "330")
                {
                    whoisLine += parsedLine.CommandMessage.Parameters[1] + " " + parsedLine.TrailMessage.TrailingContent + " " + parsedLine.CommandMessage.Parameters[2] + " ";
                    currentWhois += whoisLine + "\r\n";

                }
                else
                {
                    for (int i = 2; i < parsedLine.CommandMessage.Parameters.Count; i++)
                    {
                        whoisLine += parsedLine.CommandMessage.Command + " " + parsedLine.CommandMessage.Parameters[i] + " ";
                    }
                    currentWhois += whoisLine + parsedLine.TrailMessage.TrailingContent + "\r\n";

                }

            }
            else if (parsedLine.CommandMessage.Command == "318")
            {
                Console.WriteLine(currentWhois);
                Message msg = new Message();
                msg.Text = currentWhois;
                msg.Type = MessageType.Info;
                AddMessage(currentChannel, msg);

                currentWhois = "";
            }
            else if (parsedLine.CommandMessage.Command == "376")
            {
                if (server.nickservPassword != null && server.nickservPassword != "")
                {
                    SendMessage("nickserv", "identify " + server.nickservPassword);
                }

                if (server.channels != null && server.channels != "")
                {
                    var channelsList = server.channels.Split(',');
                    foreach (string channel in channelsList)
                    {
                        JoinChannel(channel);
                    }
                }
            }
            else
            {
                if (!parsedLine.PrefixMessage.IsUser)
                {
                    if (!channelList.Contains("Server"))
                    {
                        await AddChannel("Server");
                    }

                    Message msg = new Message();
                    msg.Text = parsedLine.OriginalMessage;
                    msg.Type = MessageType.Info;
                    msg.User = "";
                    AddMessage("Server", msg);
                }
            }
        }


        public void SendAction(string message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.Type = MessageType.Action;
            msg.User = server.username;
            AddMessage(currentChannel, msg);

            WriteLine(String.Format("PRIVMSG {0} :\u0001ACTION {1}\u0001", currentChannel, message));
        }

        public void SendMessage(string channel, string message)
        {
            Message msg = new Message();

            msg.Text = message;
            msg.User = server.username;
            msg.Type = MessageType.Normal;

            if (channelBuffers.Keys.Contains(channel))
                channelBuffers[channel].Add(msg);

            WriteLine(String.Format("PRIVMSG {0} :{1}", channel, message));
        }

        public string GetChannelTopic(string channel)
        {
            if (channelStore.ContainsKey(channel))
                return channelStore[channel].Topic;

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
            msg.User = server.username;
            msg.Type = MessageType.Normal;

            AddMessage(currentChannel, msg);

            WriteLine(String.Format("PRIVMSG {0} :{1}", currentChannel, message));
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
            if (server == null)
            {
                return;
            }

            if (!channelBuffers.ContainsKey(channel))
            {
                await AddChannel(channel);
            }

            channelBuffers[channel].Add(msg);
        }

        public async Task<bool> AddChannel(string channel)
        {
            if (channel == "")
            {
                return false;
            }

            if (!channelBuffers.Keys.Contains(channel) && !channelStore.Keys.Contains(channel) && !channelList.Contains(channel))
            {
                channelStore.Add(channel, new ChannelStore(channel));
                channelBuffers.Add(channel, new ObservableCollection<Message>());

                var comparer = Comparer<String>.Default;

                int i = 0;

                while (i < channelList.Count && comparer.Compare(channelList[i].Name.ToLower(), channel.ToLower()) < 0)
                    i++;

                channelList.Insert(i, channel);
            }

            await Task.Delay(1);

            if (!Config.Contains(Config.SwitchOnJoin))
            {
                Config.SetBoolean(Config.SwitchOnJoin, true);
            }

            return channelList.Contains(channel);
        }

        public void RemoveChannel(string channel)
        {
            if (channelBuffers.Keys.Contains(channel))
            {
                channelBuffers.Remove(channel);
                channelList.Remove(channel);
                channelStore.Remove(channel);

                if (currentChannel == channel)
                {
                    currentChannel = "";
                }
            }
        }

        public void SwitchChannel(string channel)
        {
            if (channel == null)
            {
                return;
            }

            if (channelBuffers.Keys.Contains(channel))
            {
                currentChannel = channel;
                ircMessages = channelBuffers[channel];
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

        public void ClientMessage(string text)
        {
            Message msg = new Message();
            msg.User = "";
            msg.Type = MessageType.Info;
            msg.Text = text;

            this.AddMessage(currentChannel, msg);
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
            if (!channelStore.ContainsKey(channel))
            {
                return new ObservableCollection<string>();
            }

            var store = channelStore[channel];

            if (store.SortedUsers.Count == 0)
                store.SortUsers();

            return store.SortedUsers;
        }

        public ObservableCollection<string> GetRawUsers(string channel)
        {
            return channelStore[channel].RawUsers;
        }

    }
}
