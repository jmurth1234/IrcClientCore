using System.Linq;
using System.Threading.Tasks;
using IrcClientCore.Handlers.BuiltIn;
using IrcClientCore.Tests.Helpers;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for NAMES list handling (353/366) and userhost-in-names (IRCv3.2).
    /// https://modern.ircdocs.horse/#rplnamreply-353
    /// https://ircv3.net/specs/extensions/userhost-in-names
    /// </summary>
    public class NamesHandlerTests : IAsyncLifetime
    {
        private TestableIrc _irc = null!;
        private CapHandler _capHandler = null!;

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
            _capHandler = _irc.HandlerManager.GetHandler<CapHandler>();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── Basic 353 + 366 ───────────────────────────────────────────────────────

        [Fact]
        public async Task Names_353And366_PopulatesChannelUsers()
        {
            await _irc.AddChannel("#channel");

            await _irc.SimulateReceive(":server 353 testuser = #channel :@op regular +voice");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            Assert.Equal(3, users.Count);
        }

        [Fact]
        public async Task Names_366_ChannelAutoCreatedIfMissing()
        {
            // Channel doesn't exist yet when 353 arrives
            await _irc.SimulateReceive(":server 353 testuser = #newchan :op1 op2");
            await _irc.SimulateReceive(":server 366 testuser #newchan :End of /NAMES list.");

            Assert.True(_irc.ChannelList.Contains("#newchan"));
        }

        [Fact]
        public async Task Names_Multiple353Messages_AllUsersPresent()
        {
            await _irc.AddChannel("#bigchan");

            await _irc.SimulateReceive(":server 353 testuser = #bigchan :user1 user2 user3");
            await _irc.SimulateReceive(":server 353 testuser = #bigchan :user4 user5");
            await _irc.SimulateReceive(":server 366 testuser #bigchan :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#bigchan");
            Assert.Equal(5, users.Count);
        }

        [Fact]
        public async Task Names_353_OpPrefix_Preserved()
        {
            await _irc.AddChannel("#channel");
            await _irc.SimulateReceive(":server 353 testuser = #channel :@opuser");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            var opUser = users.FirstOrDefault(u => u.Nick == "opuser");
            Assert.NotNull(opUser);
            Assert.Equal("@", opUser!.Prefix);
        }

        [Fact]
        public async Task Names_353_VoicePrefix_Preserved()
        {
            await _irc.AddChannel("#channel");
            await _irc.SimulateReceive(":server 353 testuser = #channel :+voiceuser");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            var voiceUser = users.FirstOrDefault(u => u.Nick == "voiceuser");
            Assert.NotNull(voiceUser);
            Assert.Equal("+", voiceUser!.Prefix);
        }

        [Fact]
        public async Task Names_353_MultiPrefix_HighestPrefixKept()
        {
            // multi-prefix: @+nick means op AND voice — keep the leading @
            await _irc.AddChannel("#channel");
            await _irc.SimulateReceive(":server 353 testuser = #channel :@+multiprefixuser");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "multiprefixuser");
            Assert.NotNull(user);
            // The full username should start with @ (highest prefix)
            Assert.StartsWith("@", user!.FullUsername);
        }

        [Fact]
        public async Task Names_353_PlainNick_NoPrefixSet()
        {
            await _irc.AddChannel("#channel");
            await _irc.SimulateReceive(":server 353 testuser = #channel :plainuser");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "plainuser");
            Assert.NotNull(user);
            Assert.Equal("", user!.Prefix);
        }

        // ── userhost-in-names (IRCv3.2) ───────────────────────────────────────────

        [Fact]
        public async Task Names_UserHostInNames_OpNickWithHostname_ParsedCorrectly()
        {
            _capHandler.SupportsUserHostInNames = true;
            await _irc.AddChannel("#channel");

            await _irc.SimulateReceive(":server 353 testuser = #channel :@nick!user@host");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "nick");
            Assert.NotNull(user);
            Assert.Equal("@", user!.Prefix);
        }

        [Fact]
        public async Task Names_UserHostInNames_PlainNickWithHostname_ParsedCorrectly()
        {
            _capHandler.SupportsUserHostInNames = true;
            await _irc.AddChannel("#channel");

            await _irc.SimulateReceive(":server 353 testuser = #channel :nick!user@host");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "nick");
            Assert.NotNull(user);
            Assert.Equal("", user!.Prefix);
        }

        [Fact]
        public async Task Names_UserHostInNames_MultiPrefixWithHostname_ParsedCorrectly()
        {
            _capHandler.SupportsUserHostInNames = true;
            await _irc.AddChannel("#channel");

            // @+nick!user@host — op and voiced with full host
            await _irc.SimulateReceive(":server 353 testuser = #channel :@+nick!user@host");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "nick");
            Assert.NotNull(user);
            Assert.StartsWith("@", user!.FullUsername);
        }

        [Fact]
        public async Task Names_UserHostInNames_MultipleUsers_AllParsed()
        {
            _capHandler.SupportsUserHostInNames = true;
            await _irc.AddChannel("#channel");

            await _irc.SimulateReceive(":server 353 testuser = #channel :@alpha!a@host1 +beta!b@host2 gamma!c@host3");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            Assert.Equal(3, users.Count);
            Assert.Contains(users, u => u.Nick == "alpha");
            Assert.Contains(users, u => u.Nick == "beta");
            Assert.Contains(users, u => u.Nick == "gamma");
        }

        [Fact]
        public async Task Names_WithoutUserHostInNames_HostnameNotStripped()
        {
            // Without cap, nicks are treated literally — no host stripping
            _capHandler.SupportsUserHostInNames = false;
            await _irc.AddChannel("#channel");

            await _irc.SimulateReceive(":server 353 testuser = #channel :@op plainuser");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");

            var users = _irc.GetChannelUsers("#channel");
            Assert.Equal(2, users.Count);
        }
    }
}
