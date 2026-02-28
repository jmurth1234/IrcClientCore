using System;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles STS (Strict Transport Security) - IRCv3.3
    /// Format: STS=duration=port=1234,port=5678
    ///
    /// Note: Full STS support requires reconnecting via HTTPS and upgrading to TLS,
    /// which requires significant changes to the socket layer. This handler provides
    /// awareness of STS policy changes and can trigger reconnections.
    /// </summary>
    class STSHandler : BaseHandler
    {
        public static event Action<int, string> OnSTSReceived;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            // STS is advertised in CAP LS
            // Format: sts=duration=123456,port=6697
            // We parse this in CapHandler when we see the sts capability

            return true;
        }

        /// <summary>
        /// Parse STS policy string
        /// </summary>
        public static STSInfo ParseSTS(string stsValue)
        {
            var info = new STSInfo();

            if (string.IsNullOrEmpty(stsValue))
                return info;

            var parts = stsValue.Split(',');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length != 2)
                    continue;

                switch (keyValue[0])
                {
                    case "duration":
                        if (int.TryParse(keyValue[1], out var duration))
                            info.Duration = duration;
                        break;
                    case "port":
                        if (int.TryParse(keyValue[1], out var port))
                            info.Ports.Add(port);
                        break;
                    case "preload":
                        info.Preload = keyValue[1] == "true";
                        break;
                }
            }

            return info;
        }
    }

    /// <summary>
    /// STS policy information
    /// </summary>
    public class STSInfo
    {
        public int Duration { get; set; } // Duration in seconds
        public System.Collections.Generic.List<int> Ports { get; set; } = new System.Collections.Generic.List<int>();
        public bool Preload { get; set; }

        public DateTime ExpiresAt => DateTime.UtcNow.AddSeconds(Duration);
    }
}
