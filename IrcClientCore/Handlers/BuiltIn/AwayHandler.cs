using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles away notifications (IRCv3.2)
    /// - 301: RPL_AWAY - User is away
    /// - 305: RPL_UNAWAY - You are no longer away
    /// - 306: RPL_NOWAWAY - You are now away
    /// - AWAY command (for away-notify capability)
    /// </summary>
    class AwayHandler : BaseHandler
    {
        // Event for away status changes (can be used by consuming applications)
        public static event Action<string, string> OnUserAway;
        public static event Action<string> OnUserBack;
        public static event Action OnSelfUnaway;
        public static event Action OnSelfNowaway;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var command = parsedLine.CommandMessage.Command;

            switch (command)
            {
                case "301": // RPL_AWAY
                    return await HandleAway(parsedLine);
                case "305": // RPL_UNAWAY
                    return await HandleUnaway(parsedLine);
                case "306": // RPL_NOWAWAY
                    return await HandleNowaway(parsedLine);
                case "AWAY": // Away notification (away-notify capability)
                    return await HandleAwayNotification(parsedLine);
            }

            return true;
        }

        private Task<bool> HandleAway(IrcMessage parsedLine)
        {
            // Format: :server 301 nickname :message
            var nick = parsedLine.CommandMessage.Parameters.Count > 1
                ? parsedLine.CommandMessage.Parameters[1]
                : "";
            var message = parsedLine.TrailMessage.TrailingContent;

            // Add to info buffer
            Irc.ClientMessage("Server", $"{nick} is away: {message}");

            return Task.FromResult(true);
        }

        private Task<bool> HandleUnaway(IrcMessage parsedLine)
        {
            // Format: :server 305 :You are no longer marked as away
            var message = parsedLine.TrailMessage.TrailingContent;

            Irc.ClientMessage("Server", message);
            OnSelfUnaway?.Invoke();

            return Task.FromResult(true);
        }

        private Task<bool> HandleNowaway(IrcMessage parsedLine)
        {
            // Format: :server 306 :You have been marked as away
            var message = parsedLine.TrailMessage.TrailingContent;

            Irc.ClientMessage("Server", message);
            OnSelfNowaway?.Invoke();

            return Task.FromResult(true);
        }

        private Task<bool> HandleAwayNotification(IrcMessage parsedLine)
        {
            // Format: :nick!user@host AWAY :message
            // Or just :nick!user@host AWAY (when coming back)
            var nick = parsedLine.PrefixMessage.Nickname;
            var hasMessage = parsedLine.TrailMessage.HasTrail;

            if (hasMessage)
            {
                // User went away
                var message = parsedLine.TrailMessage.TrailingContent;
                Irc.ClientMessage("Server", $"{nick} is now away: {message}");
                OnUserAway?.Invoke(nick, message);

                // Update user status in all channels
                UpdateUserAwayStatus(nick, true, message);
            }
            else
            {
                // User came back
                Irc.ClientMessage("Server", $"{nick} is no longer away");
                OnUserBack?.Invoke(nick);

                // Update user status in all channels
                UpdateUserAwayStatus(nick, false);
            }

            return Task.FromResult(true);
        }

        private void UpdateUserAwayStatus(string nick, bool isAway, string message = null)
        {
            foreach (var channel in Irc.ChannelList)
            {
                if (channel.Store.HasUser(nick))
                {
                    var user = channel.Store.Users.FirstOrDefault(u => u.Nick == nick);
                    if (user != null)
                    {
                        user.IsAway = isAway;
                        user.AwayMessage = message;
                    }
                }
            }
        }
    }
}
