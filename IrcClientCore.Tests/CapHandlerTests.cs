using System.Linq;
using System.Threading.Tasks;
using IrcClientCore.Handlers.BuiltIn;
using IrcClientCore.Tests.Helpers;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for CAP negotiation compliance with IRCv3 spec.
    /// https://ircv3.net/specs/extensions/capability-negotiation
    /// </summary>
    public class CapHandlerTests : IAsyncLifetime
    {
        private TestableIrc _irc = null!;
        private CapHandler _capHandler = null!;

        public async Task InitializeAsync()
        {
            var server = new IrcServer
            {
                Username = "testuser",
                Password = "secret",
                Hostname = "irc.example.com",
                Name = "TestServer"
            };
            _irc = new TestableIrc(server);
            await _irc.InitialiseAsync();
            _capHandler = _irc.HandlerManager.GetHandler<CapHandler>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── CAP LS: single-line ───────────────────────────────────────────────────

        [Fact]
        public async Task CapLs_ServerTime_RequestedInCapReq()
        {
            await _irc.SimulateReceive(":server CAP * LS :server-time");
            Assert.Contains(_irc.MockSocket.SentLines, l => l.Contains("server-time"));
        }

        [Fact]
        public async Task CapLs_MultiPrefix_RequestedInCapReq()
        {
            await _irc.SimulateReceive(":server CAP * LS :multi-prefix");
            Assert.Contains(_irc.MockSocket.SentLines, l => l.Contains("multi-prefix"));
        }

        [Fact]
        public async Task CapLs_EchoMessage_SetsSupportsMessageTags()
        {
            await _irc.SimulateReceive(":server CAP * LS :echo-message");
            Assert.True(_irc.SupportsMessageTags);
        }

        [Fact]
        public async Task CapLs_MessageTags_SetsSupportsMessageTags()
        {
            await _irc.SimulateReceive(":server CAP * LS :message-tags");
            Assert.True(_irc.SupportsMessageTags);
        }

        [Fact]
        public async Task CapLs_ZncSelfMessage_SetsSupportsMessageTags()
        {
            await _irc.SimulateReceive(":server CAP * LS :znc.in/self-message");
            Assert.True(_irc.SupportsMessageTags);
        }

        [Fact]
        public async Task CapLs_Znc_SetsBouncer()
        {
            await _irc.SimulateReceive(":server CAP * LS :znc");
            Assert.True(_irc.Bouncer);
        }

        [Fact]
        public async Task CapLs_Sasl_SetsSupportsSasl()
        {
            await _irc.SimulateReceive(":server CAP * LS :sasl");
            Assert.True(_capHandler.SupportsSASL);
        }

        [Fact]
        public async Task CapLs_Sasl_WithPassword_DoesNotSendCapEndImmediately()
        {
            // When SASL is supported and server has password, must hold CAP END until auth done
            _irc.MockSocket.SentLines.Clear();
            await _irc.SimulateReceive(":server CAP * LS :sasl");
            Assert.DoesNotContain("CAP END", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task CapLs_Sasl_WithoutPassword_SendsCapEnd()
        {
            _irc.Server.Password = "";
            _irc.MockSocket.SentLines.Clear();
            await _irc.SimulateReceive(":server CAP * LS :sasl");
            Assert.Contains("CAP END", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task CapLs_Sasl_WithoutPassword_DoesNotRequestSasl()
        {
            _irc.Server.Password = "";
            _irc.MockSocket.SentLines.Clear();

            await _irc.SimulateReceive(":server CAP * LS :sasl server-time");

            Assert.DoesNotContain(_irc.MockSocket.SentLines,
                l => l.StartsWith("CAP REQ") && l.Contains("sasl"));
        }

        [Fact]
        public async Task CapLs_NoSasl_SendsCapEnd()
        {
            _irc.MockSocket.SentLines.Clear();
            await _irc.SimulateReceive(":server CAP * LS :server-time multi-prefix");
            Assert.Contains("CAP END", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task CapLs_AwayNotify_SetsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :away-notify");
            Assert.True(_capHandler.SupportsAwayNotify);
        }

        [Fact]
        public async Task CapLs_Monitor_SetsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :monitor");
            Assert.True(_capHandler.SupportsMonitor);
        }

        [Fact]
        public async Task CapLs_Batch_SetsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :batch");
            Assert.True(_capHandler.SupportsBatch);
        }

        [Fact]
        public async Task CapLs_Chghost_SetsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :chghost");
            Assert.True(_capHandler.SupportsChgHost);
        }

        [Fact]
        public async Task CapLs_Setname_SetsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :setname");
            Assert.True(_capHandler.SupportsSetName);
        }

        [Fact]
        public async Task CapLs_AccountNotify_SetsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :account-notify");
            Assert.True(_capHandler.SupportsAccountNotify);
        }

        [Fact]
        public async Task CapLs_ExtendedJoin_SetsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :extended-join");
            Assert.True(_capHandler.SupportsExtendedJoin);
        }

        [Fact]
        public async Task CapLs_UserHostInNames_SetsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :userhost-in-names");
            Assert.True(_capHandler.SupportsUserHostInNames);
        }

        [Fact]
        public async Task CapLs_LabeledResponse_Requested()
        {
            await _irc.SimulateReceive(":server CAP * LS :labeled-response");
            Assert.Contains(_irc.MockSocket.SentLines, l => l.Contains("labeled-response"));
        }

        [Fact]
        public async Task CapLs_AccountTag_Requested()
        {
            await _irc.SimulateReceive(":server CAP * LS :account-tag");
            Assert.Contains(_irc.MockSocket.SentLines, l => l.Contains("account-tag"));
        }

        [Fact]
        public async Task CapLs_InviteNotify_Requested()
        {
            await _irc.SimulateReceive(":server CAP * LS :invite-notify");
            Assert.Contains(_irc.MockSocket.SentLines, l => l.Contains("invite-notify"));
        }

        [Fact]
        public async Task CapLs_Sts_DetectedButNotRequested()
        {
            // STS is security policy — detect but never send in CAP REQ
            _irc.MockSocket.SentLines.Clear();
            await _irc.SimulateReceive(":server CAP * LS :sts");
            Assert.True(_capHandler.SupportsSTS);
            Assert.DoesNotContain(_irc.MockSocket.SentLines,
                l => l.StartsWith("CAP REQ") && l.Contains("sts"));
        }

        [Fact]
        public async Task CapLs_ExtendedMonitor_Requested()
        {
            await _irc.SimulateReceive(":server CAP * LS :extended-monitor");
            Assert.Contains(_irc.MockSocket.SentLines, l => l.Contains("extended-monitor"));
        }

        // ── CAP LS: multiline (CAP LS 302 continuation) ───────────────────────────

        [Fact]
        public async Task CapLs_Multiline_ContinuationLineBuffered_FinalLineProcessed()
        {
            // First line has * indicating continuation
            await _irc.SimulateReceive(":server CAP * LS * :server-time");
            // No CAP REQ yet (still accumulating)
            var capReqAfterFirst = _irc.MockSocket.SentLines.Where(l => l.StartsWith("CAP REQ")).ToList();
            Assert.Empty(capReqAfterFirst);

            // Final line (no *)
            await _irc.SimulateReceive(":server CAP * LS :multi-prefix sasl");

            // Both caps should be requested
            var capReq = _irc.MockSocket.SentLines.FirstOrDefault(l => l.StartsWith("CAP REQ"));
            Assert.NotNull(capReq);
            Assert.Contains("server-time", capReq);
            Assert.Contains("multi-prefix", capReq);
        }

        [Fact]
        public async Task CapLs_Multiline_AllCapsDetectedAcrossLines()
        {
            await _irc.SimulateReceive(":server CAP * LS * :away-notify chghost");
            await _irc.SimulateReceive(":server CAP * LS :setname account-notify");

            Assert.True(_capHandler.SupportsAwayNotify);
            Assert.True(_capHandler.SupportsChgHost);
            Assert.True(_capHandler.SupportsSetName);
            Assert.True(_capHandler.SupportsAccountNotify);
        }

        // ── CAP ACK ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task CapAck_Sasl_StartsAuthentication()
        {
            _irc.MockSocket.SentLines.Clear();
            await _irc.SimulateReceive(":server CAP * ACK :sasl");
            Assert.Contains("AUTHENTICATE PLAIN", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task CapAck_Sasl_SetsIsAuthenticatingWithSasl()
        {
            await _irc.SimulateReceive(":server CAP * ACK :sasl");
            Assert.True(_capHandler.IsAuthenticatingWithSASL);
        }

        [Fact]
        public async Task CapAck_Sasl_WithoutPassword_DoesNotStartAuthentication()
        {
            _irc.Server.Password = "";
            _irc.MockSocket.SentLines.Clear();

            await _irc.SimulateReceive(":server CAP * ACK :sasl");

            Assert.DoesNotContain("AUTHENTICATE PLAIN", _irc.MockSocket.SentLines);
            Assert.False(_capHandler.IsAuthenticatingWithSASL);
        }

        // ── CAP NAK ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task CapNak_Sasl_ClearsSupportsSaslAndSendsCapEnd()
        {
            // First set up SASL as supported
            await _irc.SimulateReceive(":server CAP * LS :sasl");
            _irc.MockSocket.SentLines.Clear();

            await _irc.SimulateReceive(":server CAP * NAK :sasl");

            Assert.False(_capHandler.SupportsSASL);
            Assert.Contains("CAP END", _irc.MockSocket.SentLines);
        }

        // ── CAP NEW ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task CapNew_AwayNotify_RequestedAndFlagSet()
        {
            _irc.MockSocket.SentLines.Clear();
            await _irc.SimulateReceive(":server CAP * NEW :away-notify");
            Assert.True(_capHandler.SupportsAwayNotify);
            Assert.Contains(_irc.MockSocket.SentLines, l => l.Contains("away-notify"));
        }

        [Fact]
        public async Task CapNew_AlreadySupportedCap_NotRequestedAgain()
        {
            // Already supports away-notify from LS
            await _irc.SimulateReceive(":server CAP * LS :away-notify");
            _irc.MockSocket.SentLines.Clear();

            // NEW announces it again — should NOT re-request
            await _irc.SimulateReceive(":server CAP * NEW :away-notify");
            Assert.DoesNotContain(_irc.MockSocket.SentLines, l => l.StartsWith("CAP REQ"));
        }

        // ── CAP DEL ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task CapDel_AwayNotify_ClearsFlag()
        {
            await _irc.SimulateReceive(":server CAP * LS :away-notify");
            Assert.True(_capHandler.SupportsAwayNotify);

            await _irc.SimulateReceive(":server CAP * DEL :away-notify");
            Assert.False(_capHandler.SupportsAwayNotify);
        }

        [Fact]
        public async Task CapDel_MultipleFlags_AllCleared()
        {
            await _irc.SimulateReceive(":server CAP * LS :chghost setname batch monitor");
            await _irc.SimulateReceive(":server CAP * DEL :chghost setname batch monitor");

            Assert.False(_capHandler.SupportsChgHost);
            Assert.False(_capHandler.SupportsSetName);
            Assert.False(_capHandler.SupportsBatch);
            Assert.False(_capHandler.SupportsMonitor);
        }

        // ── CAP value parsing (e.g. sasl=PLAIN,EXTERNAL) ─────────────────────────

        [Fact]
        public async Task CapLs_SaslWithValue_DetectedCorrectly()
        {
            await _irc.SimulateReceive(":server CAP * LS :sasl=PLAIN,EXTERNAL");
            Assert.True(_capHandler.SupportsSASL);
        }

        [Fact]
        public async Task CapLs_StsWithValue_DetectedCorrectly()
        {
            await _irc.SimulateReceive(":server CAP * LS :sts=port=6697,duration=2764800");
            Assert.True(_capHandler.SupportsSTS);
        }
    }
}
