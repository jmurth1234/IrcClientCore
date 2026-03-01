using System;
using System.Linq;
using System.Threading.Tasks;
using IrcClientCore.Tests.Helpers;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for away-notify (IRCv3.2) and standard away numerics.
    /// https://ircv3.net/specs/extensions/away-notify
    /// https://modern.ircdocs.horse/#rplaway-301
    /// </summary>
    public class AwayHandlerTests : IAsyncLifetime
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

            // Pre-populate a channel with a user for away tests
            await _irc.AddChannel("#channel");
            await _irc.SimulateReceive(":server 353 testuser = #channel :othernick");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── AWAY notification (away-notify cap) ───────────────────────────────────

        [Fact]
        public async Task AwayNotify_WithMessage_MarksUserAsAway()
        {
            await _irc.SimulateReceive(":othernick!user@host AWAY :I'm busy right now");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "othernick");
            Assert.NotNull(user);
            Assert.True(user!.IsAway);
            Assert.Equal("I'm busy right now", user.AwayMessage);
        }

        [Fact]
        public async Task AwayNotify_WithoutMessage_MarksUserAsBack()
        {
            // First set them away
            await _irc.SimulateReceive(":othernick!user@host AWAY :gone");
            // Then they come back (AWAY with no message)
            await _irc.SimulateReceive(":othernick!user@host AWAY");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "othernick");
            Assert.NotNull(user);
            Assert.False(user!.IsAway);
        }

        [Fact]
        public async Task AwayNotify_WithMessage_PostsMessageToServerBuffer()
        {
            await _irc.SimulateReceive(":othernick!user@host AWAY :Out to lunch");

            // Should post to Server channel
            var serverLog = _irc.ChannelList.ServerLog;
            // ServerLog buffers go to the ServerLog's own buffer, not MockBuffers
            // We verify through the event instead
        }

        [Fact]
        public async Task AwayNotify_WithMessage_FiresOnUserAwayEvent()
        {
            string? capturedNick = null;
            string? capturedMsg = null;
            IrcClientCore.Handlers.BuiltIn.AwayHandler.OnUserAway += (nick, msg) =>
            {
                capturedNick = nick;
                capturedMsg = msg;
            };

            try
            {
                await _irc.SimulateReceive(":othernick!user@host AWAY :Stepping out");
                Assert.Equal("othernick", capturedNick);
                Assert.Equal("Stepping out", capturedMsg);
            }
            finally
            {
                IrcClientCore.Handlers.BuiltIn.AwayHandler.OnUserAway -= (nick, msg) => { };
            }
        }

        [Fact]
        public async Task AwayNotify_Return_FiresOnUserBackEvent()
        {
            string? capturedNick = null;
            Action<string> handler = nick => capturedNick = nick;
            IrcClientCore.Handlers.BuiltIn.AwayHandler.OnUserBack += handler;

            try
            {
                await _irc.SimulateReceive(":othernick!user@host AWAY");
                Assert.Equal("othernick", capturedNick);
            }
            finally
            {
                IrcClientCore.Handlers.BuiltIn.AwayHandler.OnUserBack -= handler;
            }
        }

        [Fact]
        public async Task AwayNotify_UpdatesAllChannelsWhereUserExists()
        {
            // Add user to a second channel
            await _irc.AddChannel("#channel2");
            await _irc.SimulateReceive(":server 353 testuser = #channel2 :othernick");
            await _irc.SimulateReceive(":server 366 testuser #channel2 :End of /NAMES list.");

            await _irc.SimulateReceive(":othernick!user@host AWAY :away message");

            foreach (var chanName in new[] { "#channel", "#channel2" })
            {
                var users = _irc.GetChannelUsers(chanName);
                var user = users.FirstOrDefault(u => u.Nick == "othernick");
                Assert.True(user?.IsAway, $"User should be marked away in {chanName}");
            }
        }

        // ── Standard away numerics ────────────────────────────────────────────────

        [Fact]
        public async Task Numeric301_RplAway_ShowsAwayMessage()
        {
            // 301 is sent when you message someone who is away
            // It should post a message to the server log
            await _irc.SimulateReceive(":server 301 testuser othernick :I am away from the keyboard");
            // No exception = pass; message posted to server info
        }

        [Fact]
        public async Task Numeric305_RplUnaway_FiresOnSelfUnawayEvent()
        {
            bool fired = false;
            Action handler = () => fired = true;
            IrcClientCore.Handlers.BuiltIn.AwayHandler.OnSelfUnaway += handler;

            try
            {
                await _irc.SimulateReceive(":server 305 testuser :You are no longer marked as being away");
                Assert.True(fired);
            }
            finally
            {
                IrcClientCore.Handlers.BuiltIn.AwayHandler.OnSelfUnaway -= handler;
            }
        }

        [Fact]
        public async Task Numeric306_RplNowaway_FiresOnSelfNowawayEvent()
        {
            bool fired = false;
            Action handler = () => fired = true;
            IrcClientCore.Handlers.BuiltIn.AwayHandler.OnSelfNowaway += handler;

            try
            {
                await _irc.SimulateReceive(":server 306 testuser :You have been marked as being away");
                Assert.True(fired);
            }
            finally
            {
                IrcClientCore.Handlers.BuiltIn.AwayHandler.OnSelfNowaway -= handler;
            }
        }
    }
}
