using System;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles account-notify (IRCv3.2)
    /// Notifies when users change their account (identified services account)
    ///
    /// This is typically handled through metadata on JOIN/PART/NICK,
    /// but some servers also send ACCOUNT command explicitly
    /// Format: :nick!user@host ACCOUNT :accountname
    /// </summary>
    class AccountNotifyHandler : BaseHandler
    {
        public static event Action<string, string> OnAccountChanged;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var command = parsedLine.CommandMessage.Command;

            // Handle explicit ACCOUNT command (some servers send this)
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
            // Format: :nick!user@host ACCOUNT :accountname
            // Or: :nick!user@host ACCOUNT :* (logged out)
            var nick = parsedLine.PrefixMessage.Nickname;
            var account = parsedLine.TrailMessage.TrailingContent;

            if (account == "*")
            {
                // User logged out / is no longer identified
                Irc.ClientMessage("Server", $"{nick} is no longer identified to an account");
                OnAccountChanged?.Invoke(nick, null);
            }
            else
            {
                // User is identified to account
                Irc.ClientMessage("Server", $"{nick} is identified to account: {account}");
                OnAccountChanged?.Invoke(nick, account);
            }

            return Task.FromResult(true);
        }

        private Task<bool> HandleAccountMetadata(IrcMessage parsedLine, string account)
        {
            // This is handled in the individual handlers (JoinHandler, PartHandler, NickHandler)
            // but we can also handle any standalone account notifications here

            var nick = parsedLine.PrefixMessage?.Nickname;
            if (!string.IsNullOrEmpty(nick))
            {
                if (account == "*")
                {
                    OnAccountChanged?.Invoke(nick, null);
                }
                else
                {
                    OnAccountChanged?.Invoke(nick, account);
                }
            }

            return Task.FromResult(true);
        }
    }
}
