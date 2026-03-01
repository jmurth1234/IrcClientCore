using System.Linq;
using System.Threading.Tasks;
using IrcClientCore.Tests.Helpers;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for connection registration flow per Modern IRC spec.
    /// https://modern.ircdocs.horse/#connection-registration
    /// </summary>
    public class ConnectionTests : IAsyncLifetime
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
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void AttemptAuth_SendsCapLs302First()
        {
            _irc.AttemptAuth();
            // CAP LS 302 must be sent as part of capability negotiation
            Assert.Contains("CAP LS 302", _irc.MockSocket.SentLines);
        }

        [Fact]
        public void AttemptAuth_SendsNickCommand()
        {
            _irc.AttemptAuth();
            Assert.Contains("NICK testuser", _irc.MockSocket.SentLines);
        }

        [Fact]
        public void AttemptAuth_SendsUserCommand_WithCorrectFormat()
        {
            // Per spec: USER <username> <mode> * :<realname>
            // Mode 8 = invisible
            _irc.AttemptAuth();
            Assert.Contains("USER testuser 8 * :testuser", _irc.MockSocket.SentLines);
        }

        [Fact]
        public void AttemptAuth_WithPassword_SendsPassCommand()
        {
            _irc.Server.Password = "secret123";
            _irc.MockSocket.SentLines.Clear();
            _irc.AttemptAuth();
            Assert.Contains("PASS secret123", _irc.MockSocket.SentLines);
        }

        [Fact]
        public void AttemptAuth_WithoutPassword_DoesNotSendPassCommand()
        {
            _irc.Server.Password = "";
            _irc.MockSocket.SentLines.Clear();
            _irc.AttemptAuth();
            Assert.DoesNotContain(_irc.MockSocket.SentLines, l => l.StartsWith("PASS"));
        }

        [Fact]
        public void AttemptAuth_CapLs302_SentBeforeNickAndUser()
        {
            _irc.AttemptAuth();
            var capIdx = _irc.MockSocket.SentLines.IndexOf("CAP LS 302");
            var nickIdx = _irc.MockSocket.SentLines.IndexOf("NICK testuser");
            Assert.True(capIdx >= 0, "CAP LS 302 not found");
            Assert.True(nickIdx >= 0, "NICK not found");
            Assert.True(capIdx < nickIdx, "CAP LS 302 should be sent before NICK");
        }

        [Fact]
        public void AttemptAuth_SetsIsAuthed()
        {
            _irc.AttemptAuth();
            Assert.True(_irc.IsAuthed);
        }
    }
}
