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
        private ObservableCollection<Message> channelBuffers;
        private IrcSocket socket;

        public static void Main(string[] args)
        {
            new Program().Start();
        }

        private void Start()
        {
            IrcServer server = new IrcServer()
            {
                name = "Test server",
                hostname = ReadLine.Read("Server Hostname: "),
                port = Convert.ToInt32(ReadLine.Read("Server Port: ", 6667.ToString())),
                username = ReadLine.Read("Username: "),
                channels = "#rymate"
            };
            ReadLine.PasswordMode = true;
            if (server.hostname.Contains("znc")) server.password += server.username + ":";
            server.password += ReadLine.Read("Password: ");
            ReadLine.PasswordMode = false;

            socket = new IrcSocket(server);

            socket.Connect();

            CommandManager handler = socket.CommandManager;

            handler.RegisterCommand("/switch", new SwitchCommand(this));

            ReadLine.AutoCompletionHandler = (text, index) =>
            {
                if (text.StartsWith("/"))
                {
                    var array = text.Split(" ");
                    var current = array[0];

                    if (array.Length > 1)
                    {
                        return handler.GetCompletions(array[0], array.Last());
                    }
                    else
                    {
                        var commands = handler.CommandList.Where(cmd => cmd.StartsWith(current));
                        return commands.ToArray();
                    }
                }
                else if (((text.StartsWith("/") && index > 0) || !text.StartsWith("/")) && socket.currentChannel != null)
                {
                    var users = socket.GetRawUsers(socket.currentChannel);
                    var current = text.Split(" ").Last();
                    return users.Where(cmd => cmd.StartsWith(current)).ToArray();
                }

                return new string[0];
            };

            SwitchChannel("Server");

            while (!socket.ReadOrWriteFailed)
            {
                var prefix = socket.currentChannel != null
                    ? $"[{socket.currentChannel} ({socket.GetChannelUsers(socket.currentChannel).Count})] "
                    : "";

                var line = ReadLine.Read($"{prefix}> "); // Get string from user
                if (line == "") continue;
                handler.HandleCommand(line);
            }
        }

        internal void SwitchChannel(string channel)
        {
            if (channelBuffers != null)
            {
                channelBuffers.CollectionChanged -= ChannelBuffersOnCollectionChanged;
            }

            socket.currentChannel = channel;
            channelBuffers = socket.channelBuffers[socket.currentChannel];
            socket.channelStore[socket.currentChannel].SortUsers();
            PrintMessages(channelBuffers);

            channelBuffers.CollectionChanged += ChannelBuffersOnCollectionChanged;
        }

        private void ChannelBuffersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            PrintMessages(args.NewItems.OfType<Message>());
        }

        private void PrintMessages(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                Console.WriteLine($"[{message.Timestamp}] {message.User} {message.Text}");
            }
        }
    }

    internal class SwitchCommand : BaseCommand
    {
        private Program program;

        public SwitchCommand(Program program)
        {
            this.program = program;
        }

        public override void RunCommand(string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            program.SwitchChannel(args[1]);
        }

        public override string[] GetCompletions(string word)
        {
            var channels = Irc.channelList.Select(channel => channel.Name);
            var completions = channels.Where(name => name.ToLower().Contains(word.ToLower()));
            return completions.ToArray();
        }
    }
}
