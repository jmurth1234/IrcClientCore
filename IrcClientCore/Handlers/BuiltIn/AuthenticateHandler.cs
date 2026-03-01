using System;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles SASL authentication (IRCv3.2)
    /// Supports PLAIN authentication mechanism
    /// </summary>
    class AuthenticateHandler : BaseHandler
    {
        private const int ChunkSize = 400;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var command = parsedLine.CommandMessage.Command;

            // Handle AUTHENTICATE command from server
            if (command == "AUTHENTICATE")
            {
                var capHandler = Irc.HandlerManager.GetHandler<CapHandler>();
                if (capHandler != null && capHandler.IsAuthenticatingWithSASL)
                {
                    // Check if server is ready for our response (+ means ready)
                    // The "+" may be in parameters or trail depending on server
                    var data = parsedLine.CommandMessage.Parameters?.Count > 0
                        ? parsedLine.CommandMessage.Parameters[0]
                        : parsedLine.TrailMessage.TrailingContent;

                    if (!string.IsNullOrEmpty(data) && data.Equals("+"))
                    {
                        // Server is ready for our response
                        var saslPlain = BuildSaslPlain();
                        await SendAuthenticateChunked(saslPlain);
                    }
                }

                return true;
            }

            // Handle SASL numerics
            switch (command)
            {
                case "903": // SASL success
                {
                    var capHandler = Irc.HandlerManager.GetHandler<CapHandler>();
                    if (capHandler != null)
                    {
                        capHandler.IsAuthenticatingWithSASL = false;
                    }
                    await Irc.WriteLine("CAP END");
                    break;
                }
                case "904": // SASL failure (invalid credentials)
                case "905": // SASL failure (too long)
                {
                    var capHandler = Irc.HandlerManager.GetHandler<CapHandler>();
                    if (capHandler != null)
                    {
                        capHandler.IsAuthenticatingWithSASL = false;
                    }
                    Irc.ClientMessage("Server", "SASL authentication failed");
                    await Irc.WriteLine("CAP END");
                    break;
                }
                case "906": // SASL aborted
                {
                    var capHandler = Irc.HandlerManager.GetHandler<CapHandler>();
                    if (capHandler != null)
                    {
                        capHandler.IsAuthenticatingWithSASL = false;
                    }
                    Irc.ClientMessage("Server", "SASL authentication aborted");
                    await Irc.WriteLine("CAP END");
                    break;
                }
                case "907": // Already authenticated
                {
                    Irc.ClientMessage("Server", "Already authenticated via SASL");
                    await Irc.WriteLine("CAP END");
                    break;
                }
                case "908": // Available SASL mechanisms
                {
                    var mechanisms = parsedLine.TrailMessage.TrailingContent;
                    Irc.ClientMessage("Server", $"Available SASL mechanisms: {mechanisms}");
                    break;
                }
            }

            return true;
        }

        private async Task SendAuthenticateChunked(string payload)
        {
            // Split into 400-byte chunks
            for (int i = 0; i < payload.Length; i += ChunkSize)
            {
                var chunk = payload.Substring(i, Math.Min(ChunkSize, payload.Length - i));
                await Irc.WriteLine($"AUTHENTICATE {chunk}");
            }

            // If the last chunk was exactly 400 bytes, send AUTHENTICATE + to signal end
            if (payload.Length > 0 && payload.Length % ChunkSize == 0)
            {
                await Irc.WriteLine("AUTHENTICATE +");
            }
        }

        private string BuildSaslPlain()
        {
            // SASL PLAIN format: base64_encode("\0username\0password")
            var username = Irc.Server.Username;
            var password = Irc.Server.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                username = "";
                password = "";
            }

            var plain = $"\0{username}\0{password}";
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            return Convert.ToBase64String(plainBytes);
        }
    }
}
