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
            
        }
    }
}
