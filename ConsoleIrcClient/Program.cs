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
        private ObservableCollection<Message> _channelBuffers;
        private Irc _socket;

        private string CurrentChannel;
        
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
                Ssl = ReadLine.Read("Use SSL: ").StartsWith('y'),
                IgnoreCertErrors = true,
                Username = ReadLine.Read("Username: "),
                Channels = "#rymate"
            };

            ReadLine.PasswordMode = true;
            server.Password += ReadLine.Read("Password: ");
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
                        var completions = handler.GetCompletions(CurrentChannel, array[0], array.Last());
                        return completions.Length > 0 ? completions : GetUserCompletions(text);
                    }

                    var commands = handler.CommandList.Where(cmd => cmd.StartsWith(current));
                    return commands.Select(command => command.Replace("/", "")).ToArray();
                }

                if ((text.StartsWith("/") && index > 0 || !text.StartsWith("/")) && CurrentChannel != null)
                {
                    return GetUserCompletions(text);
                }

                return new string[0];
            };

            SwitchChannel("");

            while (!_socket.ReadOrWriteFailed)
            {
                var prefix = CurrentChannel != null
                    ? $"[{CurrentChannel} ({_socket.GetChannelUsers(CurrentChannel).Count})] "
                    : "";

                var line = ReadLine.Read($"{prefix}> "); // Get string from user
                if (line == "") continue;
                handler.HandleCommand(CurrentChannel, line);
            }
        }

        private string[] GetUserCompletions(string text)
        {
            var users = _socket.GetRawUsers(CurrentChannel);
            var current = text.Split(" ").Last();
            return users.Where(cmd => cmd.StartsWith(current)).ToArray();
        }

        internal void SwitchChannel(string channel)
        {
            if (_channelBuffers != null)
            {
                _channelBuffers.CollectionChanged -= ChannelBuffersOnCollectionChanged;
            }
            CurrentChannel = channel;

            if (channel == "")
            {
                _channelBuffers = _socket.ChannelList.ServerLog.Buffers;
            }
            else
            {
                _channelBuffers = _socket.ChannelList[CurrentChannel].Buffers;
                _socket.ChannelList[CurrentChannel].Store.SortUsers();

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

        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length != 2)
            {
                _program.SwitchChannel("");
                ClientMessage(channel, "List of channels: " + GetCompletions(channel, ""));
                return;
            }

            _program.SwitchChannel(args[1]);
        }

        public override string[] GetCompletions(string channelName, string word)
        {
            var channels = Irc.ChannelList.Select(channel => channel.Name);
            var completions = channels.Where(name => name.ToLower().Contains(word.ToLower()));
            return completions.ToArray();
        }
    }
}
