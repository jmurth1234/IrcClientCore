using System;
using System.Linq;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles account-notify (IRCv3.2)
    /// Format: :nick!user@host ACCOUNT :accountname
    /// </summary>
    class AccountNotifyHandler : BaseHandler
    {
        public static event Action<string, string> OnAccountChanged;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var command = parsedLine.CommandMessage.Command;

            if (command == "ACCOUNT")
            {
                return await HandleAccountCommand(parsedLine);
            }

            // Check for account in message metadata (comes with JOIN/PART/NICK)
            if (parsedLine.Metadata.TryGetValue("account", out var account))
            {
                return await HandleAccountMetadata(parsedLine, account);
            }

            return true;
        }

        private Task<bool> HandleAccountCommand(IrcMessage parsedLine)
        {
            var nick = parsedLine.PrefixMessage.Nickname;
            var account = parsedLine.TrailMessage.TrailingContent;

            string accountValue;
            if (account == "*")
            {
                accountValue = null;
                Irc.ClientMessage("Server", $"{nick} is no longer identified to an account");
            }
            else
            {
                accountValue = account;
                Irc.ClientMessage("Server", $"{nick} is identified to account: {account}");
            }

            // Update user objects across all channels
            foreach (var channel in Irc.ChannelList)
            {
                if (channel.Store.HasUser(nick))
                {
                    var user = channel.Store.Users.FirstOrDefault(u => u.Nick == nick);
                    if (user != null)
                    {
                        user.Account = accountValue;
                    }
                }
            }

            OnAccountChanged?.Invoke(nick, accountValue);
            return Task.FromResult(true);
        }

        private Task<bool> HandleAccountMetadata(IrcMessage parsedLine, string account)
        {
            var nick = parsedLine.PrefixMessage?.Nickname;
            if (!string.IsNullOrEmpty(nick))
            {
                var accountValue = account == "*" ? null : account;
                OnAccountChanged?.Invoke(nick, accountValue);
            }

            return Task.FromResult(true);
        }
    }
}
