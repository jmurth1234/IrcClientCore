using IrcClientCore;
using IrcClientCore.Commands;
using IrcClientCore.Handlers.BuiltIn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using Buffer = IrcClientCore.Buffer;

namespace ConsoleIrcClient
{
    public class Program
    {
        private ObservableCollection<Message> _channelBuffers;
        private Irc _socket;

        private string _currentChannel;
        private AutocompleteHandler _autocompleteHandler;
        private bool _loggingEnabled = false;
        private string _logDirectory = "logs";

        public static void Main(string[] args)
        {
            new Program().Start();
        }

        public static bool Prompt(string prompt)
        {
            return ReadLine.Read($"{prompt} ").StartsWith('y');
        }

        /// <summary>
        /// Main loop for the CLI demo of the IrcClientCore
        /// </summary>
        private void Start()
        {
            // Create logs directory if it doesn't exist
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            
            _loggingEnabled = Prompt("Enable logging to files?");
            
            IrcServer server = null;
            if (Prompt("Load Server?"))
            {
                var name = ReadLine.Read("Server Name: ");
                server = Serialize.DeserializeObject<IrcServer>(name);
            }

            if (server == null)
            {
                Console.WriteLine("Creating server...");
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
            
            Console.WriteLine($"Connecting to {server.Hostname}");

            _socket = new Irc(server);
            _socket.Initialise();

            _socket.Connect();
            ((Buffer)_socket.InfoBuffer).Collection.CollectionChanged += ChannelBuffersOnCollectionChanged;

            var handler = _socket.CommandManager;

            handler.RegisterCommand("/switch", new SwitchCommand(this));
            handler.RegisterCommand("/reconnect", new ReconnectCommand());
            handler.RegisterCommand("/users", new UsersCommand());
            handler.RegisterCommand("/log", new LogCommand(this));
            handler.RegisterCommand("/channels", new ListCommand(this));
            handler.RegisterCommand("/commands", new CommandsHelpCommand());
            handler.RegisterCommand("/away", new AwayCommand());
            handler.RegisterCommand("/back", new BackCommand());
            handler.RegisterCommand("/reply", new ReplyCommand());
            handler.RegisterCommand("/mentions", new MentionsCommand());
            handler.RegisterCommand("/motd", new MotdCommand());

            _autocompleteHandler = new AutocompleteHandler(handler);

            ReadLine.AutoCompletionHandler = _autocompleteHandler;

            SwitchChannel("");

            _socket.HandleDisplayChannelList = HandleChannelList;

            while (!_socket.ReadOrWriteFailed)
            {
                var prefix = _currentChannel != null
                    ? $"[{_currentChannel} ({_socket.GetChannelUsers(_currentChannel).Count})] "
                    : "";

                var line = ReadLine.Read($"{prefix}> "); // Get string from user
                if (line == "") continue;

                handler.HandleCommand(_currentChannel, line);
            }
        }

        private void HandleChannelList(List<ChannelListItem> list)
        {
            if (Debugger.IsAttached) Debugger.Break();
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
                _channelBuffers = ((Buffer) _socket.ChannelList.ServerLog.Buffers).Collection;
            }
            else
            {
                _channelBuffers = ((Buffer)_socket.ChannelList[_currentChannel].Buffers).Collection;
            }

            PrintMessages(_channelBuffers);

            if (_channelBuffers != null) _channelBuffers.CollectionChanged += ChannelBuffersOnCollectionChanged;
        }

        private void ChannelBuffersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            PrintMessages(args.NewItems.OfType<Message>());
            
            // Log messages if logging is enabled
            if (_loggingEnabled && args.NewItems != null)
            {
                LogMessages(args.NewItems.OfType<Message>().ToList(), _currentChannel);
            }
        }

        private static void PrintMessages(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                var id = message.MessageId != null ? $"{{ID: {message.MessageId.Substring(0, 6)}}}" : "";

                // Save the current console color
                var originalColor = Console.ForegroundColor;

                // Print timestamp in gray
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{message.Timestamp}] ");

                // Print message ID in dark cyan if exists
                if (!string.IsNullOrEmpty(id))
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write($"{id} ");
                }

                // Show reply indicator
                if (!string.IsNullOrEmpty(message.ReplyTo))
                {
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write($"[reply:{message.ReplyTo.Substring(0, Math.Min(6, message.ReplyTo.Length))}] ");
                }

                // Color-code by message type
                switch (message.Type)
                {
                    case MessageType.Info:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("<");
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write($"{message.User}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("> ");
                        break;

                    case MessageType.Action:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"* {message.User} ");
                        break;

                    case MessageType.Notice:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("<");
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write($"{message.User}");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("> ");
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("<");
                        Console.ForegroundColor = message.Mention ? ConsoleColor.Magenta : ConsoleColor.Cyan;
                        Console.Write($"{message.User}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("> ");
                        break;
                }

                // Print message text with IRC formatting (ANSI colors)
                Console.ForegroundColor = originalColor;
                AnsiFormatter.WriteFormatted(message);
                Console.WriteLine();
            }
        }

        private void LogMessages(List<Message> messages, string channel)
        {
            if (string.IsNullOrEmpty(channel))
            {
                channel = "server";
            }
            
            string logFileName = Path.Combine(_logDirectory, $"{_socket.Server.Name}_{channel.Replace("#", "")}.log");
            
            try
            {
                using (StreamWriter writer = File.AppendText(logFileName))
                {
                    foreach (var message in messages)
                    {
                        writer.WriteLine($"[{message.Timestamp}] <{message.User}> {message.Text}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public void ToggleLogging()
        {
            _loggingEnabled = !_loggingEnabled;
            Console.WriteLine($"Logging is now {(_loggingEnabled ? "enabled" : "disabled")}");
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
            var originalColor = Console.ForegroundColor;

            Console.WriteLine($"\n=== Users in {channel} ({chan.Store.Users.Count}) ===");

            foreach (var user in chan.Store.Users)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"  {user.FullUsername}");

                if (!string.IsNullOrEmpty(user.Account))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write($" [{user.Account}]");
                }

                if (user.IsAway)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(" (away");
                    if (!string.IsNullOrEmpty(user.AwayMessage))
                        Console.Write($": {user.AwayMessage}");
                    Console.Write(")");
                }

                Console.WriteLine();
            }

            Console.WriteLine("===============");
            Console.ForegroundColor = originalColor;
        }
    }

    internal class LogCommand : BaseCommand
    {
        private readonly Program _program;

        public LogCommand(Program program)
        {
            _program = program;
        }

        public override void RunCommand(string channel, string[] args)
        {
            _program.ToggleLogging();
        }
    }

    internal class ListCommand : BaseCommand
    {
        private readonly Program _program;

        public ListCommand(Program program)
        {
            _program = program;
        }

        public override void RunCommand(string channel, string[] args)
        {
            // Get all channels from the IRC client
            var channels = Irc.ChannelList;
            
            // Save original color
            var originalColor = Console.ForegroundColor;
            
            Console.WriteLine("\n=== Channels ===");
            
            foreach (var chan in channels)
            {
                // Skip the server log
                if (chan.Name == "Server") continue;
                
                // Display channel name in cyan
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{chan.Name}");
                
                // Display user count in gray
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" ({chan.Store.Users.Count} users)");
                
                // Display topic in yellow if available
                if (!string.IsNullOrEmpty(chan.Store.Topic))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    // Truncate long topics
                    var topic = chan.Store.Topic.Length > 50 ? chan.Store.Topic.Substring(0, 47) + "..." : chan.Store.Topic;
                    Console.Write($" - {topic}");
                }
                
                Console.WriteLine();
            }
            
            Console.WriteLine("===============");
            
            // Restore original color
            Console.ForegroundColor = originalColor;
        }
    }

    internal class AwayCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            var message = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "Away";
            Irc.SetAway(message);
            ClientMessage(channel, $"You are now away: {message}");
        }
    }

    internal class BackCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            Irc.SetBack();
            ClientMessage(channel, "You are no longer away");
        }
    }

    internal class ReplyCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (args.Length < 3)
            {
                ClientMessage(channel, "Usage: /reply <message-id> <message>");
                return;
            }

            var replyTo = args[1];
            var message = string.Join(" ", args.Skip(2));
            Irc.SendReply(channel, message, replyTo);
        }
    }

    internal class MentionsCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            var originalColor = Console.ForegroundColor;

            if (Irc.Mentions.Count == 0)
            {
                ClientMessage(channel, "No mentions");
                return;
            }

            Console.WriteLine($"\n=== Mentions ({Irc.Mentions.Count}) ===");

            foreach (var msg in Irc.Mentions)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{msg.Timestamp}] ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"{msg.Channel} ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"<{msg.User}> ");
                Console.ForegroundColor = originalColor;
                Console.WriteLine(msg.Text);
            }

            Console.WriteLine("===============");
            Console.ForegroundColor = originalColor;
        }
    }

    internal class MotdCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            if (string.IsNullOrEmpty(Irc.MOTD))
            {
                ClientMessage(channel, "No MOTD available");
                return;
            }

            Console.WriteLine(Irc.MOTD);
        }
    }

    internal class CommandsHelpCommand : BaseCommand
    {
        public override void RunCommand(string channel, string[] args)
        {
            // Save original color
            var originalColor = Console.ForegroundColor;

            Console.WriteLine("\n=== Available Commands ===");

            // Display standard IRC commands
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nStandard IRC Commands:");
            Console.ForegroundColor = ConsoleColor.White;

            PrintCommand("/join #channel", "Join a channel");
            PrintCommand("/part #channel", "Leave a channel");
            PrintCommand("/msg nick message", "Send a private message");
            PrintCommand("/me action", "Perform an action");
            PrintCommand("/nick newnick", "Change your nickname");
            PrintCommand("/quit [message]", "Disconnect from the server");
            PrintCommand("/list", "Request channel list from server");
            PrintCommand("/away [message]", "Set yourself as away");
            PrintCommand("/back", "Return from away");

            // Display client-specific commands
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nClient Commands:");
            Console.ForegroundColor = ConsoleColor.White;

            PrintCommand("/switch [channel]", "Switch to a channel or show channel list");
            PrintCommand("/channels", "Show formatted list of joined channels");
            PrintCommand("/users", "Show users in current channel (with away/account info)");
            PrintCommand("/reply <id> <msg>", "Reply to a specific message by ID");
            PrintCommand("/mentions", "Show all messages that mentioned you");
            PrintCommand("/motd", "Show the server's Message of the Day");
            PrintCommand("/log", "Toggle logging to files");
            PrintCommand("/reconnect", "Reconnect to the server");
            PrintCommand("/commands", "Display this help message");
            PrintCommand("/help", "Show list of available commands");

            Console.WriteLine("\n========================");

            // Restore original color
            Console.ForegroundColor = originalColor;
        }

        private void PrintCommand(string command, string description)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{command}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" - {description}");
        }
    }
}
