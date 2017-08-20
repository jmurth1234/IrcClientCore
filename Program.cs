using System;

namespace dotnet_irc_testing
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            IrcMessage msg = new IrcMessage(":james000-!~james000_@ns354110.ip-91-121-101.eu PRIVMSG #rymate :OUTRAAAGGEEEEE");

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
            
            while (true) 
            {
                Console.Write("> "); // Prompt
                string line = Console.ReadLine(); // Get string from user
                handler.HandleCommand(socket, line);
            }
        }
    }
}
