using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;

namespace dotnet_irc_testing
{
    class Program
    {
        private static ObservableCollection<Message> channelBuffers;

        static void Main(string[] args)
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
            server.password = ReadLine.Read("Password: ");
            ReadLine.PasswordMode = false;

            IrcSocket socket = new IrcSocket(server);

            socket.Connect();

            CommandHandler handler = new CommandHandler();
            handler.RegisterCommand("/switch", CurrentChannelCommandHandler);
            ReadLine.AutoCompletionHandler = (text, index) =>
            {
                if (text.StartsWith("/"))
                {
                    var current = text.Split(" ")[0];
                    var commands = handler.GetCommands().Where(cmd => cmd.StartsWith(current));
                    return commands.ToArray();
                }
                else if (((text.StartsWith("/") && index > 0) || !text.StartsWith("/")) && socket.currentChannel != null)
                {
                    var users = socket.GetRawUsers(socket.currentChannel);
                    var current = text.Split(" ").Last();
                    return users.Where(cmd => cmd.StartsWith(current)).ToArray();
                }
                
                return new string[0];
            };
            
            while (!socket.ReadOrWriteFailed)
            {
                var prefix = socket.currentChannel != null
                    ? $"[{socket.currentChannel} ({socket.GetChannelUsers(socket.currentChannel).Count})] "
                    : "";

                var line = ReadLine.Read($"{prefix}> "); // Get string from user
                handler.HandleCommand(socket, line);
            }
        }

        private static void CurrentChannelCommandHandler(Irc irc, string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            if (channelBuffers != null)
            {
                channelBuffers.CollectionChanged -= ChannelBuffersOnCollectionChanged;
            }

            irc.currentChannel = args[1];
            channelBuffers = irc.channelBuffers[irc.currentChannel];
            irc.channelStore[irc.currentChannel].SortUsers();
            PrintMessages(channelBuffers);

            channelBuffers.CollectionChanged += ChannelBuffersOnCollectionChanged;
        }

        private static void ChannelBuffersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
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
}
