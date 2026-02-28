using System;
using System.Linq;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles SETNAME (realname change) notifications (IRCv3.2)
    /// Format: :nick!user@host SETNAME :new real name
    /// </summary>
    class SetNameHandler : BaseHandler
    {
        public static event Action<string, string> OnNameChanged;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            if (parsedLine.CommandMessage.Command != "SETNAME")
            {
                return true;
            }

            var nick = parsedLine.PrefixMessage.Nickname;
            var newRealName = parsedLine.TrailMessage.TrailingContent;

            // Update user objects and notify all channels
            foreach (var channel in Irc.ChannelList)
            {
                if (channel.Store.HasUser(nick))
                {
                    var user = channel.Store.Users.FirstOrDefault(u => u.Nick == nick);
                    if (user != null)
                    {
                        user.RealName = newRealName;
                    }

                    var msg = new Message
                    {
                        Type = MessageType.Info,
                        User = nick,
                        Text = $"Changed real name to: {newRealName}"
                    };
                    Irc.AddMessage(channel.Name, msg);
                }
            }

            OnNameChanged?.Invoke(nick, newRealName);
            return true;
        }
    }
}
