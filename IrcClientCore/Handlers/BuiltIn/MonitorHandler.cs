using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles MONITOR system (IRCv3.2)
    /// - 730: RPL_MONONLINE - Monitor target is online
    /// - 731: RPL_MONOFFLINE - Monitor target is offline
    /// - 732: RPL_MONLIST - List of monitored nicknames
    /// - 733: RPL_ENDOFMONLIST - End of monitor list
    /// - 734: ERR_MONLISTFULL - Monitor list is full
    /// - 735: RPL_MONITORING - Currently monitoring
    /// - 736: ERR_MONDISABLED - MONITOR is disabled
    /// </summary>
    class MonitorHandler : BaseHandler
    {
        // Event for user online/offline status changes
        public static event Action<string> OnUserOnline;
        public static event Action<string> OnUserOffline;

        // Monitor numerics
        private static readonly string[] _monitorCmds = new string[] { "730", "731", "732", "733", "734", "735", "736" };

        public MonitorHandler()
        {
            foreach (var cmd in _monitorCmds)
            {
                Commands.Add(cmd);
            }
        }

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var command = parsedLine.CommandMessage.Command;

            switch (command)
            {
                case "730": // RPL_MONONLINE
                    return await HandleMonOnline(parsedLine);
                case "731": // RPL_MONOFFLINE
                    return await HandleMonOffline(parsedLine);
                case "732": // RPL_MONLIST
                    return await HandleMonList(parsedLine);
                case "733": // RPL_ENDOFMONLIST
                    return await HandleEndOfMonList(parsedLine);
                case "734": // ERR_MONLISTFULL
                    return await HandleMonListFull(parsedLine);
                case "735": // RPL_MONITORING
                    return await HandleMonitoring(parsedLine);
                case "736": // ERR_MONDISABLED
                    return await HandleMonDisabled(parsedLine);
            }

            return true;
        }

        private Task<bool> HandleMonOnline(IrcMessage parsedLine)
        {
            // Format: :server 730 target :nick1!user1@host1,nick2!user2@host2,...
            // Or: :server 730 target :nick (simple format)
            var params_ = parsedLine.CommandMessage.Parameters;
            if (params_.Count < 2)
            {
                return Task.FromResult(true);
            }

            var targets = parsedLine.TrailMessage.TrailingContent;

            // Multiple targets can be comma-separated
            foreach (var target in targets.Split(','))
            {
                var nick = target.Contains("!")
                    ? target.Split('!')[0]
                    : target.Trim();

                Irc.ClientMessage("Server", $"{nick} is now online (monitor)");
                OnUserOnline?.Invoke(nick);
            }

            return Task.FromResult(true);
        }

        private Task<bool> HandleMonOffline(IrcMessage parsedLine)
        {
            // Format: :server 731 target :nick1,nick2,...
            var params_ = parsedLine.CommandMessage.Parameters;
            if (params_.Count < 2)
            {
                return Task.FromResult(true);
            }

            var targets = parsedLine.TrailMessage.TrailingContent;

            // Multiple targets can be comma-separated
            foreach (var target in targets.Split(','))
            {
                Irc.ClientMessage("Server", $"{target} is now offline (monitor)");
                OnUserOffline?.Invoke(target.Trim());
            }

            return Task.FromResult(true);
        }

        private Task<bool> HandleMonList(IrcMessage parsedLine)
        {
            // Format: :server 732 target :nick1,nick2,...
            var targets = parsedLine.TrailMessage.TrailingContent;
            Irc.ClientMessage("Server", $"Monitor list: {targets}");
            return Task.FromResult(true);
        }

        private Task<bool> HandleEndOfMonList(IrcMessage parsedLine)
        {
            // Format: :server 733 target :End of MONITOR list
            var message = parsedLine.TrailMessage.TrailingContent;
            Irc.ClientMessage("Server", message);
            return Task.FromResult(true);
        }

        private Task<bool> HandleMonListFull(IrcMessage parsedLine)
        {
            // Format: :server 734 target list :Monitor list is full
            var message = parsedLine.TrailMessage.TrailingContent;
            Irc.ClientMessage("Server", $"Monitor error: {message}");
            return Task.FromResult(true);
        }

        private Task<bool> HandleMonitoring(IrcMessage parsedLine)
        {
            // Format: :server 735 target :nick1!user1@host1,nick2!user2@host2,...
            var message = parsedLine.TrailMessage.TrailingContent;
            Irc.ClientMessage("Server", $"Currently monitoring: {message}");
            return Task.FromResult(true);
        }

        private Task<bool> HandleMonDisabled(IrcMessage parsedLine)
        {
            // Format: :server 736 * :MONITOR is disabled
            var message = parsedLine.TrailMessage.TrailingContent;
            Irc.ClientMessage("Server", $"Monitor error: {message}");
            return Task.FromResult(true);
        }
    }
}
