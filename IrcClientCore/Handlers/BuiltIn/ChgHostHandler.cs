using System;
using System.Linq;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles CHGHOST (hostname change) notifications (IRCv3.2)
    /// Format: :nick!user@oldhost CHGHOST newuser newhost
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

            // Format: :nick!user@oldhost CHGHOST newuser newhost
            var nick = parsedLine.PrefixMessage.Nickname;
            var oldHost = parsedLine.PrefixMessage.Hostname;
            var parameters = parsedLine.CommandMessage.Parameters;

            string newUser = parameters?.Count > 0 ? parameters[0] : null;
            string newHost = parameters?.Count > 1 ? parameters[1] : parsedLine.TrailMessage.TrailingContent;

            // Notify all channels and update user objects
            foreach (var channel in Irc.ChannelList)
            {
                if (channel.Store.HasUser(nick))
                {
                    var msg = new Message
                    {
                        Type = MessageType.Info,
                        User = nick,
                        Text = $"Changed host: {newUser}@{newHost}"
                    };
                    Irc.AddMessage(channel.Name, msg);
                }
            }

            OnHostChanged?.Invoke(nick, oldHost, newHost);
            return true;
        }
    }
}
