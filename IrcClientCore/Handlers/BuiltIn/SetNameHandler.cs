using System;
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

            // Format: :nick!user@host SETNAME :new real name
            var nick = parsedLine.PrefixMessage.Nickname;
            var newRealName = parsedLine.TrailMessage.TrailingContent;

            // Notify all channels
            foreach (var channel in Irc.ChannelList)
            {
                if (channel.Store.HasUser(nick))
                {
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
