using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class CapHandler : BaseHandler
    {
        // IRCv3 capability states (instance, not static)
        public bool SupportsSASL { get; internal set; }
        public bool SupportsAwayNotify { get; internal set; }
        public bool SupportsMonitor { get; internal set; }
        public bool SupportsBatch { get; internal set; }
        public bool SupportsChgHost { get; internal set; }
        public bool SupportsSetName { get; internal set; }
        public bool SupportsAccountNotify { get; internal set; }
        public bool SupportsExtendedJoin { get; internal set; }
        public bool SupportsUserHostInNames { get; internal set; }
        public bool SupportsSTS { get; internal set; }

        // SASL authentication state
        public bool IsAuthenticatingWithSASL { get; internal set; }
        public string SaslMechanism { get; internal set; }

        // Multiline CAP LS accumulation
        private readonly List<string> _capLsBuffer = new List<string>();

        // Track whether we're waiting for SASL to complete before sending CAP END
        private bool _saslRequested;

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

        private bool HasCap(string capList, string capName)
        {
            foreach (var token in capList.Split(' '))
            {
                var name = token.Contains("=") ? token.Substring(0, token.IndexOf('=')) : token;
                if (name.Equals(capName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private string GetCapValue(string capList, string capName)
        {
            foreach (var token in capList.Split(' '))
            {
                if (!token.Contains("=")) continue;
                var idx = token.IndexOf('=');
                var name = token.Substring(0, idx);
                if (name.Equals(capName, StringComparison.OrdinalIgnoreCase))
                    return token.Substring(idx + 1);
            }
            return null;
        }

        private async Task<bool> HandleCapLs(IrcMessage parsedLine)
        {
            // Check for multiline continuation: Parameters looks like ["*", "LS", "*"] for continuation
            // or ["*", "LS"] for final line
            var parameters = parsedLine.CommandMessage.Parameters;
            bool isContinuation = parameters.Count > 2 && parameters[2] == "*";

            var currentCaps = parsedLine.TrailMessage.TrailingContent;

            if (isContinuation)
            {
                _capLsBuffer.Add(currentCaps);
                return true;
            }

            // Final line — combine buffered caps with current
            if (_capLsBuffer.Count > 0)
            {
                _capLsBuffer.Add(currentCaps);
                currentCaps = string.Join(" ", _capLsBuffer);
                _capLsBuffer.Clear();
            }

            var compatibleFeatures = currentCaps;
            var requirements = "";

            if (HasCap(compatibleFeatures, "znc"))
            {
                Irc.Bouncer = true;
            }

            // Existing capabilities
            if (HasCap(compatibleFeatures, "znc.in/server-time-iso"))
            {
                requirements += "znc.in/server-time-iso ";
            }

            if (HasCap(compatibleFeatures, "server-time"))
            {
                requirements += "server-time ";
            }

            if (HasCap(compatibleFeatures, "multi-prefix"))
            {
                requirements += "multi-prefix ";
            }

            if (HasCap(compatibleFeatures, "echo-message"))
            {
                requirements += "echo-message ";
                Irc.SupportsMessageTags = true;
            }

            if (HasCap(compatibleFeatures, "message-tags"))
            {
                requirements += "message-tags ";
                Irc.SupportsMessageTags = true;
            }

            if (HasCap(compatibleFeatures, "znc.in/self-message"))
            {
                requirements += "znc.in/self-message ";
                Irc.SupportsMessageTags = true;
            }

            // IRCv3.2 capabilities
            if (HasCap(compatibleFeatures, "sasl"))
            {
                // Only request SASL if credentials are configured.
                SupportsSASL = true;
                if (!string.IsNullOrEmpty(Irc.Server.Password))
                {
                    requirements += "sasl ";
                }
            }

            if (HasCap(compatibleFeatures, "away-notify"))
            {
                requirements += "away-notify ";
                SupportsAwayNotify = true;
            }

            if (HasCap(compatibleFeatures, "monitor"))
            {
                requirements += "monitor ";
                SupportsMonitor = true;
            }

            if (HasCap(compatibleFeatures, "extended-monitor"))
            {
                requirements += "extended-monitor ";
            }

            if (HasCap(compatibleFeatures, "batch"))
            {
                requirements += "batch ";
                SupportsBatch = true;
            }

            if (HasCap(compatibleFeatures, "chghost"))
            {
                requirements += "chghost ";
                SupportsChgHost = true;
            }

            if (HasCap(compatibleFeatures, "setname"))
            {
                requirements += "setname ";
                SupportsSetName = true;
            }

            if (HasCap(compatibleFeatures, "account-notify"))
            {
                requirements += "account-notify ";
                SupportsAccountNotify = true;
            }

            if (HasCap(compatibleFeatures, "extended-join"))
            {
                requirements += "extended-join ";
                SupportsExtendedJoin = true;
            }

            // IRCv3.3 capabilities
            if (HasCap(compatibleFeatures, "userhost-in-names"))
            {
                requirements += "userhost-in-names ";
                SupportsUserHostInNames = true;
            }

            if (HasCap(compatibleFeatures, "labeled-response"))
            {
                requirements += "labeled-response ";
            }

            if (HasCap(compatibleFeatures, "account-tag"))
            {
                requirements += "account-tag ";
            }

            if (HasCap(compatibleFeatures, "invite-notify"))
            {
                requirements += "invite-notify ";
            }

            // STS: do NOT request via CAP REQ, just detect and set flag
            if (HasCap(compatibleFeatures, "sts"))
            {
                SupportsSTS = true;
            }

            // Request all selected capabilities
            if (!string.IsNullOrEmpty(requirements))
            {
                await Irc.WriteLine("CAP REQ :" + requirements.Trim());
            }

            // Only send CAP END now if we're not expecting SASL
            if (!SupportsSASL || string.IsNullOrEmpty(Irc.Server.Password))
            {
                await EndCapNegotiation();
            }

            return true;
        }

        private async Task<bool> HandleCapAck(IrcMessage parsedLine)
        {
            var caps = parsedLine.TrailMessage.TrailingContent;

            if (HasCap(caps, "sasl"))
            {
                if (string.IsNullOrEmpty(Irc.Server.Password))
                {
                    await EndCapNegotiation();
                    return true;
                }

                // Start SASL authentication
                IsAuthenticatingWithSASL = true;
                _saslRequested = true;
                await Irc.WriteLine("AUTHENTICATE PLAIN");
            }

            return true;
        }

        internal async Task EndCapNegotiation()
        {
            if (!_saslRequested || !IsAuthenticatingWithSASL)
            {
                await Irc.WriteLine("CAP END");
            }
        }

        private async Task<bool> HandleCapNak(IrcMessage parsedLine)
        {
            // If SASL was NAK'd, end negotiation
            var caps = parsedLine.TrailMessage.TrailingContent;
            if (HasCap(caps, "sasl"))
            {
                SupportsSASL = false;
                _saslRequested = false;
                await EndCapNegotiation();
            }
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
            // New capabilities announced by server — request any supported ones
            var newCaps = parsedLine.TrailMessage.TrailingContent;
            var requirements = "";

            if (HasCap(newCaps, "away-notify") && !SupportsAwayNotify)
            {
                requirements += "away-notify ";
                SupportsAwayNotify = true;
            }
            if (HasCap(newCaps, "account-notify") && !SupportsAccountNotify)
            {
                requirements += "account-notify ";
                SupportsAccountNotify = true;
            }
            if (HasCap(newCaps, "chghost") && !SupportsChgHost)
            {
                requirements += "chghost ";
                SupportsChgHost = true;
            }
            if (HasCap(newCaps, "setname") && !SupportsSetName)
            {
                requirements += "setname ";
                SupportsSetName = true;
            }
            if (HasCap(newCaps, "extended-join") && !SupportsExtendedJoin)
            {
                requirements += "extended-join ";
                SupportsExtendedJoin = true;
            }
            if (HasCap(newCaps, "batch") && !SupportsBatch)
            {
                requirements += "batch ";
                SupportsBatch = true;
            }
            if (HasCap(newCaps, "monitor") && !SupportsMonitor)
            {
                requirements += "monitor ";
                SupportsMonitor = true;
            }
            if (HasCap(newCaps, "extended-monitor"))
            {
                requirements += "extended-monitor ";
            }
            if (HasCap(newCaps, "userhost-in-names") && !SupportsUserHostInNames)
            {
                requirements += "userhost-in-names ";
                SupportsUserHostInNames = true;
            }
            if (HasCap(newCaps, "labeled-response"))
            {
                requirements += "labeled-response ";
            }
            if (HasCap(newCaps, "account-tag"))
            {
                requirements += "account-tag ";
            }
            if (HasCap(newCaps, "invite-notify"))
            {
                requirements += "invite-notify ";
            }

            if (!string.IsNullOrEmpty(requirements))
            {
                await Irc.WriteLine("CAP REQ :" + requirements.Trim());
            }

            return true;
        }

        private async Task<bool> HandleCapDel(IrcMessage parsedLine)
        {
            // Capabilities removed by server — clear flags
            var removedCaps = parsedLine.TrailMessage.TrailingContent;

            if (HasCap(removedCaps, "away-notify")) SupportsAwayNotify = false;
            if (HasCap(removedCaps, "account-notify")) SupportsAccountNotify = false;
            if (HasCap(removedCaps, "chghost")) SupportsChgHost = false;
            if (HasCap(removedCaps, "setname")) SupportsSetName = false;
            if (HasCap(removedCaps, "extended-join")) SupportsExtendedJoin = false;
            if (HasCap(removedCaps, "batch")) SupportsBatch = false;
            if (HasCap(removedCaps, "monitor")) SupportsMonitor = false;
            if (HasCap(removedCaps, "userhost-in-names")) SupportsUserHostInNames = false;
            if (HasCap(removedCaps, "sasl")) SupportsSASL = false;

            return true;
        }
    }
}
