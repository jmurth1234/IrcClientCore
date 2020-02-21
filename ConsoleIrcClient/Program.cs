using IrcClientCore;
using IrcClientCore.Commands;
using IrcClientCore.Handlers.BuiltIn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;

namespace ConsoleIrcClient
{
    public class Program
    {
        private ObservableCollection<Message> _channelBuffers;
        private Irc _socket;

        private string _currentChannel;
        private AutocompleteHandler _autocompleteHandler;

        public static void Main(string[] args)
        {
            new Program().Start();
        }
        public static bool AllowPrompt { get; set; }

        public static bool Prompt(string prompt)
        {
            return ReadLine.Read($"{prompt} ").StartsWith('y');
        }

        /// <summary>
        /// Main loop for the CLI demo of the IrcClientCore
        /// </summary>
        private void Start()
        {
            IrcServer server = null;
            if (Prompt("Load Server?"))
            {
                var name = ReadLine.Read("Server Name: ");
                server = Serialize.DeserializeObject<IrcServer>(name);
            }

            if (server == null)
            {
                server = new IrcServer()
                {
                    Name = ReadLine.Read("Server Name: "),
                    Hostname = ReadLine.Read("Server Hostname: "),
                    Port = Convert.ToInt32(ReadLine.Read("Server Port: ", "6667")),
                    Ssl = Prompt("Use SSL:"),
                    IgnoreCertErrors = true,
                    Username = ReadLine.Read("Username: "),
                    Password = ReadLine.ReadPassword("Password: "),
                    Channels = ReadLine.Read("Channels to join (format #channel,#other...): ")
                };
               
                if (Prompt("Save Server?"))
                {
                    Serialize.SerializeObject(server, server.Name);
                }
            }

            _socket = new IrcSocket(server);
            _socket.Initialise();

            _socket.Connect();

            var handler = _socket.CommandManager;

            handler.RegisterCommand("/switch", new SwitchCommand(this));
            handler.RegisterCommand("/reconnect", new ReconnectCommand());
            handler.RegisterCommand("/users", new UsersCommand());

            _autocompleteHandler = new AutocompleteHandler(handler);

            ReadLine.AutoCompletionHandler = _autocompleteHandler;

            SwitchChannel("");

            _socket.HandleDisplayChannelList = HandleChannelList;

            while (!_socket.ReadOrWriteFailed)
            {
                var prefix = _currentChannel != null
                    ? $"[{_currentChannel} ({_socket.GetChannelUsers(_currentChannel).Count})] "
                    : "";

                while (!AllowPrompt)
                {
                    //wait lmao
                }

                var line = ReadLine.Read($"{prefix}> "); // Get string from user
                if (line == "") continue;
                AllowPrompt = false;

                handler.HandleCommand(_currentChannel, line);
            }
        }

        private void HandleChannelList(List<ChannelListItem> list)
        {
            if (Debugger.IsAttached) Debugger.Break();
            AllowPrompt = true;
        }

        internal void SwitchChannel(string channel)
        {
            if (_channelBuffers != null)
            {
                _channelBuffers.CollectionChanged -= ChannelBuffersOnCollectionChanged;
            }
            _currentChannel = channel;
            _autocompleteHandler.CurrentChannel = channel;

            if (channel == "")
            {
                _channelBuffers = _socket.ChannelList.ServerLog.Buffers as ObservableCollection<Message>;
            }
            else
            {
                _channelBuffers = _socket.ChannelList[_currentChannel].Buffers as ObservableCollection<Message>;
            }

            PrintMessages(_channelBuffers);

            if (_channelBuffers != null) _channelBuffers.CollectionChanged += ChannelBuffersOnCollectionChanged;
        }

        private void ChannelBuffersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            PrintMessages(args.NewItems.OfType<Message>());
        }

        private static void PrintMessages(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                var id = message.MessageId != null ? $"{{ID: {message.MessageId.Substring(0, 6)}}}" : "";
                Console.WriteLine($"[{message.Timestamp}] {id} <{message.User}> {message.Text}");
            }

            AllowPrompt = true;
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
                ClientMessage(channel, "List of channels: " + string.Join(", ", GetCompletions(channel, "")));
                return;
            }

            _program.SwitchChannel(args[1]);
        }

        public override string[] GetCompletions(string channel, string word)
        {
            var channels = Irc.ChannelList.Select(chan => chan.Name);
            var completions = channels.Where(name => name.ToLower().Contains(word.ToLower()));
            return completions.ToArray();
        }
    }
    internal class ReconnectCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            Irc.DisconnectAsync(attemptReconnect: true);
        }
    }
    internal class UsersCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            var chan = Irc.ChannelList[channel];
            ClientMessage(channel, "List of users: " + string.Join(", ", chan.Store.Users));
        }
    }
}
