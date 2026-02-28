using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class CapHandler : BaseHandler
    {
        // IRCv3 capability states
        public static bool SupportsSASL { get; internal set; }
        public static bool SupportsAwayNotify { get; internal set; }
        public static bool SupportsMonitor { get; internal set; }
        public static bool SupportsBatch { get; internal set; }
        public static bool SupportsChgHost { get; internal set; }
        public static bool SupportsSetName { get; internal set; }
        public static bool SupportsAccountNotify { get; internal set; }
        public static bool SupportsExtendedJoin { get; internal set; }
        public static bool SupportsUserHostInNames { get; internal set; }
        public static bool SupportsSTS { get; internal set; }

        // SASL authentication state
        public static bool IsAuthenticatingWithSASL { get; internal set; }
        public static string SaslMechanism { get; internal set; }

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var subCommand = parsedLine.CommandMessage.Parameters.Count > 1 ? parsedLine.CommandMessage.Parameters[1] : "";

            switch (subCommand)
            {
                case "LS":
                    return await HandleCapLs(parsedLine);
                case "ACK":
                    return await HandleCapAck(parsedLine);
                case "NAK":
                    return await HandleCapNak(parsedLine);
                case "REQ":
                    return await HandleCapReq(parsedLine);
                case "LIST":
                    return await HandleCapList(parsedLine);
                case "NEW":
                    return await HandleCapNew(parsedLine);
                case "DEL":
                    return await HandleCapDel(parsedLine);
            }

            return true;
        }

        private async Task<bool> HandleCapLs(IrcMessage parsedLine)
        {
            var requirements = "";
            var compatibleFeatures = parsedLine.TrailMessage.TrailingContent;

            if (compatibleFeatures.Contains("znc"))
            {
                Irc.Bouncer = true;
            }

            // Existing capabilities
            if (compatibleFeatures.Contains("znc.in/server-time-iso"))
            {
                requirements += "znc.in/server-time-iso ";
            }

            if (compatibleFeatures.Contains("server-time"))
            {
                requirements += "server-time ";
            }

            if (compatibleFeatures.Contains("multi-prefix"))
            {
                requirements += "multi-prefix ";
            }

            if (compatibleFeatures.Contains("echo-message"))
            {
                requirements += "echo-message ";
                Irc.SupportsMessageTags = true;
            }

            if (compatibleFeatures.Contains("message-tags"))
            {
                requirements += "message-tags ";
                Irc.SupportsMessageTags = true;
            }

            if (compatibleFeatures.Contains("znc.in/self-message"))
            {
                requirements += "znc.in/self-message ";
                Irc.SupportsMessageTags = true;
            }

            // IRCv3.2 capabilities
            if (compatibleFeatures.Contains("sasl"))
            {
                requirements += "sasl ";
                SupportsSASL = true;
            }

            if (compatibleFeatures.Contains("away-notify"))
            {
                requirements += "away-notify ";
                SupportsAwayNotify = true;
            }

            if (compatibleFeatures.Contains("monitor"))
            {
                requirements += "monitor ";
                SupportsMonitor = true;
            }

            if (compatibleFeatures.Contains("batch"))
            {
                requirements += "batch ";
                SupportsBatch = true;
            }

            if (compatibleFeatures.Contains("chghost"))
            {
                requirements += "chghost ";
                SupportsChgHost = true;
            }

            if (compatibleFeatures.Contains("setname"))
            {
                requirements += "setname ";
                SupportsSetName = true;
            }

            if (compatibleFeatures.Contains("account-notify"))
            {
                requirements += "account-notify ";
                SupportsAccountNotify = true;
            }

            if (compatibleFeatures.Contains("extended-join"))
            {
                requirements += "extended-join ";
                SupportsExtendedJoin = true;
            }

            // IRCv3.3 capabilities
            if (compatibleFeatures.Contains("userhost-in-names"))
            {
                requirements += "userhost-in-names ";
                SupportsUserHostInNames = true;
            }

            if (compatibleFeatures.Contains("sts"))
            {
                requirements += "sts ";
                SupportsSTS = true;
            }

            // Request all selected capabilities
            if (!string.IsNullOrEmpty(requirements))
            {
                await Irc.WriteLine("CAP REQ :" + requirements.Trim());
            }

            // If SASL is available but wasn't in requirements, request it
            if (SupportsSASL && !requirements.Contains("sasl"))
            {
                await Irc.WriteLine("CAP REQ :sasl");
            }

            await Irc.WriteLine("CAP END");
            return true;
        }

        private async Task<bool> HandleCapAck(IrcMessage parsedLine)
        {
            var caps = parsedLine.TrailMessage.TrailingContent;

            if (caps.Contains("sasl"))
            {
                // Start SASL authentication
                IsAuthenticatingWithSASL = true;
                await Irc.WriteLine("AUTHENTICATE :PLAIN");
            }

            return true;
        }

        private async Task<bool> HandleCapNak(IrcMessage parsedLine)
        {
            // Handle capability rejection
            return true;
        }

        private async Task<bool> HandleCapReq(IrcMessage parsedLine)
        {
            // Handle server requesting capabilities from us
            return true;
        }

        private async Task<bool> HandleCapList(IrcMessage parsedLine)
        {
            // List of active capabilities
            return true;
        }

        private async Task<bool> HandleCapNew(IrcMessage parsedLine)
        {
            // New capabilities announced by server
            return true;
        }

        private async Task<bool> HandleCapDel(IrcMessage parsedLine)
        {
            // Capabilities removed by server
            return true;
        }
    }
}
