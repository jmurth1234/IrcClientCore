using System.Linq;
using System.Threading.Tasks;
using IrcClientCore.Tests.Helpers;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for outbound message formatting compliance with the IRC spec.
    /// https://modern.ircdocs.horse/#privmsg-message
    /// https://ircv3.net/specs/extensions/labeled-response
    /// </summary>
    public class IrcSendTests : IAsyncLifetime
    {
        private TestableIrc _irc = null!;

        public async Task InitializeAsync()
        {
            var server = new IrcServer
            {
                Username = "testuser",
                Password = "",
                Hostname = "irc.example.com",
                Name = "TestServer"
            };
            _irc = new TestableIrc(server);
            await _irc.InitialiseAsync();
            await _irc.AddChannel("#channel");
            _irc.MockSocket.SentLines.Clear(); // Clear setup noise
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── PRIVMSG ───────────────────────────────────────────────────────────────

        [Fact]
        public void SendMessage_FormatsCorrectly()
        {
            _irc.SendMessage("#channel", "Hello World");
            Assert.Contains("PRIVMSG #channel :Hello World", _irc.MockSocket.SentLines);
        }

        [Fact]
        public void SendMessage_ToUser_FormatsCorrectly()
        {
            _irc.SendMessage("othernick", "Hey there");
            Assert.Contains("PRIVMSG othernick :Hey there", _irc.MockSocket.SentLines);
        }

        [Fact]
        public void SendMessage_WithoutMessageTags_AddsToLocalBuffer()
        {
            _irc.SupportsMessageTags = false;
            var buf = _irc.MockBuffers["#channel"];
            var countBefore = buf.Messages.Count;

            _irc.SendMessage("#channel", "local echo");
            Assert.Equal(countBefore + 1, buf.Messages.Count);
        }

        [Fact]
        public void SendMessage_WithMessageTags_DoesNotAddToLocalBuffer()
        {
            // echo-message means server echoes back — don't double-add
            _irc.SupportsMessageTags = true;
            var buf = _irc.MockBuffers["#channel"];
            var countBefore = buf.Messages.Count;

            _irc.SendMessage("#channel", "server will echo");
            Assert.Equal(countBefore, buf.Messages.Count);
        }

        // ── CTCP ACTION ───────────────────────────────────────────────────────────

        [Fact]
        public void SendAction_FormatsCorrectly()
        {
            _irc.SendAction("#channel", "waves hello");
            // CTCP ACTION format: PRIVMSG #chan :\u0001ACTION text\u0001
            // Using \u0001 (unambiguous) not \x01 which C# may parse greedily with following hex chars
            var expected = "PRIVMSG #channel :\u0001ACTION waves hello\u0001";
            Assert.Contains(_irc.MockSocket.SentLines, l => l == expected);
        }

        // ── draft/reply (IRCv3 client tags) ──────────────────────────────────────

        [Fact]
        public void SendReply_FormatsCorrectly()
        {
            // @+draft/reply=<id> PRIVMSG <channel> :<message>
            _irc.SendReply("#channel", "That's great!", "msgid-abc123");
            Assert.Contains(_irc.MockSocket.SentLines,
                l => l == "@+draft/reply=msgid-abc123 PRIVMSG #channel :That's great!");
        }

        // ── labeled-response (IRCv3.2) ────────────────────────────────────────────

        [Fact]
        public async Task SendLabeledMessage_FormatsCorrectly()
        {
            await _irc.SendLabeledMessage("#channel", "hello", "label-xyz");
            Assert.Contains("@label=label-xyz PRIVMSG #channel :hello", _irc.MockSocket.SentLines);
        }

        // ── JOIN / PART ───────────────────────────────────────────────────────────

        [Fact]
        public async Task JoinChannel_SendsJoinCommand()
        {
            await _irc.JoinChannel("#newchan");
            Assert.Contains("JOIN #newchan", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task JoinChannel_AddsChannelToList()
        {
            await _irc.JoinChannel("#newchan2");
            Assert.True(_irc.ChannelList.Contains("#newchan2"));
        }

        [Fact]
        public void PartChannel_SendsPartCommand()
        {
            _irc.PartChannel("#channel");
            Assert.Contains("PART #channel", _irc.MockSocket.SentLines);
        }

        [Fact]
        public void PartChannel_RemovesChannelFromList()
        {
            _irc.PartChannel("#channel");
            Assert.False(_irc.ChannelList.Contains("#channel"));
        }

        // ── MONITOR commands (IRCv3.2) ────────────────────────────────────────────

        [Fact]
        public async Task MonitorAdd_FormatsCorrectly()
        {
            await _irc.MonitorAdd("watchednick");
            Assert.Contains("MONITOR + watchednick", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task MonitorRemove_FormatsCorrectly()
        {
            await _irc.MonitorRemove("watchednick");
            Assert.Contains("MONITOR - watchednick", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task MonitorList_FormatsCorrectly()
        {
            await _irc.MonitorList();
            Assert.Contains("MONITOR L", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task MonitorClear_FormatsCorrectly()
        {
            await _irc.MonitorClear();
            Assert.Contains("MONITOR C", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task MonitorStatus_FormatsCorrectly()
        {
            await _irc.MonitorStatus("checknick");
            Assert.Contains("MONITOR S checknick", _irc.MockSocket.SentLines);
        }

        // ── SETNAME (IRCv3.2) ─────────────────────────────────────────────────────

        [Fact]
        public async Task SetRealName_FormatsCorrectly()
        {
            await _irc.SetRealName("My New Name");
            Assert.Contains("SETNAME :My New Name", _irc.MockSocket.SentLines);
        }

        // ── AWAY ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task SetAway_FormatsCorrectly()
        {
            await _irc.SetAway("Out to lunch");
            Assert.Contains("AWAY :Out to lunch", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task SetBack_SendsAwayWithNoMessage()
        {
            await _irc.SetBack();
            Assert.Contains("AWAY", _irc.MockSocket.SentLines);
            // Must be plain AWAY without a message
            Assert.DoesNotContain(_irc.MockSocket.SentLines, l => l.StartsWith("AWAY :"));
        }

        // ── NICK ──────────────────────────────────────────────────────────────────

        [Fact]
        public void NickSetter_SendsNickCommand()
        {
            _irc.Nickname = "newnick";
            Assert.Contains("NICK newnick", _irc.MockSocket.SentLines);
        }

        [Fact]
        public void NickSetter_UpdatesServerUsername()
        {
            _irc.Nickname = "newnick";
            Assert.Equal("newnick", _irc.Server.Username);
        }

        // ── BATCH (client-side) ───────────────────────────────────────────────────

        [Fact]
        public async Task SendBatchStart_FormatsCorrectly()
        {
            await _irc.SendBatchStart("ref1", "draft/multiline");
            Assert.Contains("BATCH +ref1 draft/multiline", _irc.MockSocket.SentLines);
        }

        [Fact]
        public async Task SendBatchEnd_FormatsCorrectly()
        {
            await _irc.SendBatchEnd("ref1");
            Assert.Contains("BATCH -ref1", _irc.MockSocket.SentLines);
        }
    }
}
