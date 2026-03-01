using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IrcClientCore.Handlers.BuiltIn;
using IrcClientCore.Tests.Helpers;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for SASL PLAIN authentication (IRCv3 SASL spec).
    /// https://ircv3.net/specs/extensions/sasl-3.1
    /// </summary>
    public class AuthenticateHandlerTests : IAsyncLifetime
    {
        private TestableIrc _irc = null!;
        private CapHandler _capHandler = null!;

        public async Task InitializeAsync()
        {
            var server = new IrcServer
            {
                Username = "testuser",
                Password = "testpass",
                Hostname = "irc.example.com",
                Name = "TestServer"
            };
            _irc = new TestableIrc(server);
            await _irc.InitialiseAsync();
            _capHandler = _irc.HandlerManager.GetHandler<CapHandler>();

            // Set up SASL state as if CAP ACK :sasl was received
            _capHandler.IsAuthenticatingWithSASL = true;
            _irc.MockSocket.SentLines.Clear();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── AUTHENTICATE + → sends base64 PLAIN ──────────────────────────────────

        [Fact]
        public async Task Authenticate_Plus_SendsSaslPlainResponse()
        {
            await _irc.SimulateReceive(":server AUTHENTICATE +");

            var authLine = _irc.MockSocket.SentLines.FirstOrDefault(l => l.StartsWith("AUTHENTICATE ") && l != "AUTHENTICATE PLAIN");
            Assert.NotNull(authLine);
        }

        [Fact]
        public async Task Authenticate_Plus_ResponseHasNoColon()
        {
            // SASL PLAIN must NOT have a colon before the base64 payload
            await _irc.SimulateReceive(":server AUTHENTICATE +");

            var authLines = _irc.MockSocket.SentLines.Where(l => l.StartsWith("AUTHENTICATE ") && l != "AUTHENTICATE PLAIN").ToList();
            foreach (var line in authLines)
            {
                // Should be "AUTHENTICATE <base64>" not "AUTHENTICATE :<base64>"
                Assert.DoesNotMatch(@"^AUTHENTICATE :", line);
            }
        }

        [Fact]
        public async Task Authenticate_Plus_PayloadIsCorrectSaslPlainFormat()
        {
            // SASL PLAIN: base64("\0username\0password")
            await _irc.SimulateReceive(":server AUTHENTICATE +");

            var authLine = _irc.MockSocket.SentLines.FirstOrDefault(l => l.StartsWith("AUTHENTICATE ") && l != "AUTHENTICATE PLAIN");
            Assert.NotNull(authLine);

            var base64Part = authLine!.Substring("AUTHENTICATE ".Length);
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Part));
            Assert.Equal("\0testuser\0testpass", decoded);
        }

        [Fact]
        public async Task Authenticate_Plus_EmptyCredentials_SendsValidBase64()
        {
            _irc.Server.Username = "";
            _irc.Server.Password = "";
            _irc.MockSocket.SentLines.Clear();

            await _irc.SimulateReceive(":server AUTHENTICATE +");

            var authLine = _irc.MockSocket.SentLines.FirstOrDefault(l => l.StartsWith("AUTHENTICATE ") && l != "AUTHENTICATE PLAIN");
            Assert.NotNull(authLine);
            var base64Part = authLine!.Substring("AUTHENTICATE ".Length);
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Part));
            Assert.Equal("\0\0", decoded);
        }

        // ── 400-byte chunking ─────────────────────────────────────────────────────

        [Fact]
        public async Task Authenticate_LongPayload_SentInChunksOf400Bytes()
        {
            // Create credentials that will produce a base64 > 400 chars
            _irc.Server.Username = new string('a', 300);
            _irc.Server.Password = new string('b', 300);
            _irc.MockSocket.SentLines.Clear();

            await _irc.SimulateReceive(":server AUTHENTICATE +");

            var authLines = _irc.MockSocket.SentLines
                .Where(l => l.StartsWith("AUTHENTICATE ") && l != "AUTHENTICATE PLAIN")
                .ToList();

            // Should have more than one AUTHENTICATE line
            Assert.True(authLines.Count > 1, "Long payload should be split into multiple chunks");

            // Each chunk (except possibly the last) should be exactly "AUTHENTICATE " + 400 chars
            foreach (var line in authLines.Take(authLines.Count - 1))
            {
                if (line != "AUTHENTICATE +")
                {
                    var payload = line.Substring("AUTHENTICATE ".Length);
                    Assert.Equal(400, payload.Length);
                }
            }
        }

        [Fact]
        public async Task Authenticate_PayloadExactly400Bytes_SendsTrailingPlus()
        {
            // Craft a payload that is exactly 400 base64 chars
            // "\0username\0password" needs to be such that base64(payload) == 400 chars
            // 400 base64 chars = 300 raw bytes. We need \0 + user + \0 + pass = 300 bytes
            // So user=149 chars, pass=149 chars → \0(1)+149+\0(1)+149 = 300 ✓
            _irc.Server.Username = new string('u', 149);
            _irc.Server.Password = new string('p', 149);
            _irc.MockSocket.SentLines.Clear();

            await _irc.SimulateReceive(":server AUTHENTICATE +");

            // Should end with AUTHENTICATE +
            Assert.Contains("AUTHENTICATE +", _irc.MockSocket.SentLines);
        }

        // ── SASL numerics ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Numeric903_SaslSuccess_SendsCapEnd()
        {
            await _irc.SimulateReceive(":server 903 testuser :SASL authentication successful");
            Assert.Contains("CAP END", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task Numeric903_SaslSuccess_ClearsIsAuthenticatingWithSasl()
        {
            await _irc.SimulateReceive(":server 903 testuser :SASL authentication successful");
            Assert.False(_capHandler.IsAuthenticatingWithSASL);
        }

        [Fact]
        public async Task Numeric904_SaslFailure_SendsCapEnd()
        {
            await _irc.SimulateReceive(":server 904 testuser :SASL authentication failed: Invalid credentials");
            Assert.Contains("CAP END", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task Numeric905_SaslTooLong_SendsCapEnd()
        {
            await _irc.SimulateReceive(":server 905 testuser :SASL message too long");
            Assert.Contains("CAP END", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task Numeric906_SaslAborted_SendsCapEnd()
        {
            await _irc.SimulateReceive(":server 906 testuser :SASL authentication aborted");
            Assert.Contains("CAP END", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task Numeric907_AlreadyAuthenticated_SendsCapEnd()
        {
            _capHandler.IsAuthenticatingWithSASL = false; // 907 can arrive any time
            await _irc.SimulateReceive(":server 907 testuser :You are already authenticated");
            Assert.Contains("CAP END", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task Numeric908_AvailableMechanisms_ShowsMessageNoCapEnd()
        {
            // 908 is informational — shows available mechanisms, does NOT end negotiation
            await _irc.SimulateReceive(":server 908 testuser :PLAIN EXTERNAL");
            Assert.DoesNotContain("CAP END", _irc.MockSocket.SentLines);
        }
    }
}
