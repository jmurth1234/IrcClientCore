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
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            // Handle AUTHENTICATE command from server
            if (parsedLine.CommandMessage.Command == "AUTHENTICATE")
            {
                var data = parsedLine.TrailMessage.TrailingContent;

                if (CapHandler.IsAuthenticatingWithSASL)
                {
                    // Check if server is ready for our response (+ means ready)
                    if (!string.IsNullOrEmpty(data) && data.Equals("+"))
                    {
                        // Server is ready for our response
                        // Send SASL PLAIN: base64(\0username\0password)
                        var saslPlain = BuildSaslPlain();
                        await Irc.WriteLine($"AUTHENTICATE :{saslPlain}");
                    }
                    else
                    {
                        // Authentication successful or failed
                        CapHandler.IsAuthenticatingWithSASL = false;

                        // Send CAP END to finish CAP negotiation
                        await Irc.WriteLine("CAP END");
                    }
                }

                // Let other handlers process this as well (return true but don't block)
                return true;
            }

            // Handle 903 (SASL success) and 904/905 (SASL failure) numerics
            if (parsedLine.CommandMessage.Command == "903")
            {
                CapHandler.IsAuthenticatingWithSASL = false;
                await Irc.WriteLine("CAP END");
            }
            else if (parsedLine.CommandMessage.Command == "904" || parsedLine.CommandMessage.Command == "905")
            {
                // SASL authentication failed
                CapHandler.IsAuthenticatingWithSASL = false;
                await Irc.WriteLine("CAP END");
            }

            return true;
        }

        private string BuildSaslPlain()
        {
            // SASL PLAIN format: base64_encode("\0username\0password")
            var username = Irc.Server.Username;
            var password = Irc.Server.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                // If no username/password, use empty auth
                username = "";
                password = "";
            }

            var plain = $"\0{username}\0{password}";
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            return Convert.ToBase64String(plainBytes);
        }
    }
}
