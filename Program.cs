using System;
using System.Runtime.InteropServices;

namespace dotnet_irc_testing
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            IrcMessage msg =
                new IrcMessage(":james000-!~james000_@ns354110.ip-91-121-101.eu PRIVMSG #rymate :OUTRAAAGGEEEEE");

            Console.WriteLine(msg.TrailMessage.TrailingContent);

            IrcServer server = new IrcServer()
            {
                name = "Test server",
                hostname = "irc.esper.net",
                port = 6667,
                username = "testuser212",
                channels = "#rymate"
            };

            IrcSocket socket = new IrcSocket(server);

            socket.Connect();

            CommandHandler handler = new CommandHandler();
            while (!socket.ReadOrWriteFailed)
            {
                var prefix = socket.currentChannel != null
                    ? $"[{socket.currentChannel} ({socket.GetChannelUsers(socket.currentChannel).Count})] "
                    : "";

                var line = ReadLine.Read($"{prefix}> "); // Get string from user
                handler.HandleCommand(socket, line);
            }
        }
    }
}
