using IrcClientCore;
using IrcClientCore.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace ConsoleIrcClient
{
    public class Program
    {
<<<<<<< HEAD
        private ObservableCollection<Message> channelBuffers;
        private Irc socket;
=======
        private ObservableCollection<Message> _channelBuffers;
        private Irc _socket;
>>>>>>> 82c7c8c850f21fb69a505110da8979baa4b2b29d

        public static void Main(string[] args)
        {
            new Program().Start();
        }

        /// <summary>
        /// Main loop for the CLI demo of the IrcClientCore
        /// </summary>
        private void Start()
        {
            var server = new IrcServer()
            {
                Name = "Test server",
                Hostname = ReadLine.Read("Server Hostname: "),
                Port = Convert.ToInt32(ReadLine.Read("Server Port: ", "6667")),
                Ssl = Convert.ToBoolean(ReadLine.Read("Use SSL: ")),
                IgnoreCertErrors = true,
                Username = ReadLine.Read("Username: "),
                Channels = "#rymate"
            };

            ReadLine.PasswordMode = true;
<<<<<<< HEAD
            
            if (server.hostname.Contains("znc")) server.password += server.username + ":";
            server.password += ReadLine.Read("Password: ");
=======
            if (server.Hostname.Contains("znc")) server.Password += server.Username + ":";
            server.Password += ReadLine.Read("Password: ");
>>>>>>> 82c7c8c850f21fb69a505110da8979baa4b2b29d
            ReadLine.PasswordMode = false;

            _socket = new IrcSocket(server);

            _socket.Connect();

            var handler = _socket.CommandManager;

            handler.RegisterCommand("/switch", new SwitchCommand(this));

            ReadLine.AutoCompletionHandler = (text, index) =>
            {
                var array = text.Split(" ");

                if (text.StartsWith("/"))
                {
                    var current = array[0];

                    if (array.Length > 1)
                    {
                        var completions = handler.GetCompletions(array[0], array.Last());
                        return completions.Length > 0 ? completions : GetUserCompletions(text);
                    }

                    var commands = handler.CommandList.Where(cmd => cmd.StartsWith(current));
                    return commands.Select(command => command.Replace("/", "")).ToArray();
                }

                if ((text.StartsWith("/") && index > 0 || !text.StartsWith("/")) && _socket.CurrentChannel != null)
                {
                    return GetUserCompletions(text);
                }

                return new string[0];
            };

            SwitchChannel("");

            while (!_socket.ReadOrWriteFailed)
            {
                var prefix = _socket.CurrentChannel != null
                    ? $"[{_socket.CurrentChannel} ({_socket.GetChannelUsers(_socket.CurrentChannel).Count})] "
                    : "";

                var line = ReadLine.Read($"{prefix}> "); // Get string from user
                if (line == "") continue;
                handler.HandleCommand(line);
            }
        }

        private string[] GetUserCompletions(string text)
        {
            var users = _socket.GetRawUsers(_socket.CurrentChannel);
            var current = text.Split(" ").Last();
            return users.Where(cmd => cmd.StartsWith(current)).ToArray();
        }

        internal void SwitchChannel(string channel)
        {
            if (_channelBuffers != null)
            {
                _channelBuffers.CollectionChanged -= ChannelBuffersOnCollectionChanged;
            }
            _socket.CurrentChannel = channel;

            if (channel == "")
            {
                _channelBuffers = _socket.ChannelList.ServerLog.Buffers;
            }
            else
            {
                _channelBuffers = _socket.ChannelList[_socket.CurrentChannel].Buffers;
                _socket.ChannelList[_socket.CurrentChannel].Store.SortUsers();

            }

            PrintMessages(_channelBuffers);

            _channelBuffers.CollectionChanged += ChannelBuffersOnCollectionChanged;
        }

        private void ChannelBuffersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            PrintMessages(args.NewItems.OfType<Message>());
        }

        private static void PrintMessages(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                Console.WriteLine($"[{message.Timestamp}] {message.User} {message.Text}");
            }
        }
    }

    internal class SwitchCommand : BaseCommand
    {
        private readonly Program _program;

        public SwitchCommand(Program program)
        {
            this._program = program;
        }

        public override void RunCommand(string[] args)
        {
            if (args.Length != 2)
            {
                _program.SwitchChannel("");
                ClientMessage("List of channels: " + GetCompletions(""));
                return;
            }

            _program.SwitchChannel(args[1]);
        }

        public override string[] GetCompletions(string word)
        {
            var channels = Irc.ChannelList.Select(channel => channel.Name);
            var completions = channels.Where(name => name.ToLower().Contains(word.ToLower()));
            return completions.ToArray();
        }
    }
}
