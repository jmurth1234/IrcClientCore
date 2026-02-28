using System;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles CHGHOST (hostname change) notifications (IRCv3.2)
    /// Format: :nick!user@oldhost CHGHOST newhost
    /// </summary>
    class ChgHostHandler : BaseHandler
    {
        public static event Action<string, string, string> OnHostChanged;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            if (parsedLine.CommandMessage.Command != "CHGHOST")
            {
                return true;
            }

            // Format: :nick!user@oldhost CHGHOST newhost
            var nick = parsedLine.PrefixMessage.Nickname;
            var newHost = parsedLine.TrailMessage.TrailingContent;
            var oldHost = parsedLine.PrefixMessage.Hostname;

            // Notify all channels
            foreach (var channel in Irc.ChannelList)
            {
                if (channel.Store.HasUser(nick))
                {
                    var msg = new Message
                    {
                        Type = MessageType.Info,
                        User = nick,
                        Text = $"Hostname changed from {oldHost} to {newHost}"
                    };
                    Irc.AddMessage(channel.Name, msg);
                }
            }

            OnHostChanged?.Invoke(nick, oldHost, newHost);
            return true;
        }
    }
}
