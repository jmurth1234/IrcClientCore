using System;
using System.Linq;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    internal class JoinHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            string channel = null;
            string account = null;
            string realName = null;

            // Check for extended-join (IRCv3.2)
            // Format: :nick!user@host JOIN #channel account :realname
            // The account is in parameters[1] and realname in trailing
            var capHandler = Irc.HandlerManager.GetHandler<CapHandler>();
            if (capHandler != null && capHandler.SupportsExtendedJoin && parsedLine.CommandMessage.Parameters != null && parsedLine.CommandMessage.Parameters.Count >= 2)
            {
                // Extended join format: JOIN #channel account :realname
                channel = parsedLine.CommandMessage.Parameters[0];
                account = parsedLine.CommandMessage.Parameters[1]; // Account name or "*" if not logged in
                realName = parsedLine.TrailMessage.TrailingContent;
            }
            else
            {
                // Standard join can be either JOIN #channel or JOIN :#channel
                if (parsedLine.CommandMessage.Parameters != null && parsedLine.CommandMessage.Parameters.Count > 0)
                {
                    channel = parsedLine.CommandMessage.Parameters[0];
                }
                else
                {
                    channel = parsedLine.TrailMessage.TrailingContent;
                }
            }

            // Also check for account in metadata (account-notify)
            if (parsedLine.Metadata.TryGetValue("account", out var metaAccount))
            {
                account = metaAccount;
            }

            // Guard against null channel
            if (string.IsNullOrEmpty(channel))
            {
                return true;
            }

            if (parsedLine.PrefixMessage.Nickname == Irc.Server.Username)
            {
                await Irc.AddChannel(channel);
            }

            // Build user info string
            string userInfo = "";
            if (!string.IsNullOrEmpty(account) && account != "*")
            {
                userInfo = $"[{account}]";
            }

            var msg = new Message
            {
                Type = MessageType.JoinPart,
                User = parsedLine.PrefixMessage.Nickname,
                Text = string.Format("({0}) {1} {2}", parsedLine.PrefixMessage.Prefix, userInfo + "joined the channel", !string.IsNullOrEmpty(realName) ? $"({realName})" : "")
            };

            Irc.AddMessage(channel, msg);

            Irc.ChannelList[channel].Store.AddUser(parsedLine.PrefixMessage.Nickname);

            // Update user account and realname info if available
            if (!string.IsNullOrEmpty(account) || !string.IsNullOrEmpty(realName))
            {
                var user = Irc.ChannelList[channel].Store.Users.FirstOrDefault(u => u.Nick == parsedLine.PrefixMessage.Nickname);
                if (user != null)
                {
                    if (!string.IsNullOrEmpty(account) && account != "*")
                    {
                        user.Account = account;
                        Irc.ClientMessage("Server", $"{parsedLine.PrefixMessage.Nickname} is identified to account: {account}");
                    }
                    if (!string.IsNullOrEmpty(realName))
                    {
                        user.RealName = realName;
                    }
                }
            }

            return true;
        }
    }
}
