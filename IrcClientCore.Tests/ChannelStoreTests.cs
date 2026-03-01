using System.Threading.Tasks;
using IrcClientCore.Tests.Helpers;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for ChannelStore: user management, prefixes, and topic.
    /// </summary>
    public class ChannelStoreTests : IAsyncLifetime
    {
        private TestableIrc _irc = null!;
        private ChannelStore _store = null!;

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
            await _irc.AddChannel("#testchan");
            _store = _irc.ChannelList["#testchan"].Store;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── AddUser / HasUser ─────────────────────────────────────────────────────

        [Fact]
        public void AddUser_PlainNick_CanBeFound()
        {
            _store.AddUser("alice");
            Assert.True(_store.HasUser("alice"));
        }

        [Fact]
        public void AddUser_PrefixedNick_HasUserIgnoresPrefix()
        {
            _store.AddUser("@opuser");
            Assert.True(_store.HasUser("opuser"),
                "HasUser should find user by nick regardless of prefix");
        }

        [Fact]
        public void AddUser_Duplicate_NotAddedTwice()
        {
            _store.AddUser("alice");
            _store.AddUser("alice");
            Assert.Single(_store.Users);
        }

        [Fact]
        public void AddUser_EmptyString_Ignored()
        {
            _store.AddUser("");
            Assert.Empty(_store.Users);
        }

        [Fact]
        public void AddUser_Null_Ignored()
        {
            _store.AddUser(null!);
            Assert.Empty(_store.Users);
        }

        [Fact]
        public void HasUser_NonExistent_ReturnsFalse()
        {
            Assert.False(_store.HasUser("nobody"));
        }

        // ── RemoveUser ────────────────────────────────────────────────────────────

        [Fact]
        public void RemoveUser_ExistingUser_Removed()
        {
            _store.AddUser("alice");
            _store.RemoveUser("alice");
            Assert.False(_store.HasUser("alice"));
        }

        [Fact]
        public void RemoveUser_NonExistent_DoesNotThrow()
        {
            var ex = Record.Exception(() => _store.RemoveUser("nobody"));
            Assert.Null(ex);
        }

        [Fact]
        public void RemoveUser_AlsoRemovesFromRawUsers()
        {
            _store.AddUser("alice");
            _store.RemoveUser("alice");
            Assert.DoesNotContain("alice", _store.RawUsers);
        }

        // ── ChangeUser (nick change) ──────────────────────────────────────────────

        [Fact]
        public void ChangeUser_RenamesUser()
        {
            _store.AddUser("oldnick");
            _store.ChangeUser("oldnick", "newnick");
            Assert.False(_store.HasUser("oldnick"));
            Assert.True(_store.HasUser("newnick"));
        }

        [Fact]
        public void ChangeUser_PreservesPrefix()
        {
            _store.AddUser("@opuser");
            _store.ChangeUser("opuser", "newopnick");
            var user = _store.Users[0];
            Assert.Equal("@", user.Prefix);
            Assert.Equal("newopnick", user.Nick);
        }

        [Fact]
        public void ChangeUser_NonExistentUser_DoesNotThrow()
        {
            var ex = Record.Exception(() => _store.ChangeUser("nobody", "newname"));
            Assert.Null(ex);
        }

        // ── ChangePrefix ──────────────────────────────────────────────────────────

        [Fact]
        public void ChangePrefix_SetsNewPrefix()
        {
            _store.AddUser("alice");
            _store.ChangePrefix("alice", "@");
            Assert.Equal("@", _store.GetPrefix("alice"));
        }

        [Fact]
        public void ChangePrefix_RemovesPrefix()
        {
            _store.AddUser("@alice");
            _store.ChangePrefix("alice", "");
            Assert.Equal("", _store.GetPrefix("alice"));
        }

        // ── ReplaceUsers ──────────────────────────────────────────────────────────

        [Fact]
        public void ReplaceUsers_ClearsPreviousUsers()
        {
            _store.AddUser("old1");
            _store.AddUser("old2");
            _store.ReplaceUsers(new System.Collections.Generic.List<string> { "new1" });
            Assert.False(_store.HasUser("old1"));
            Assert.False(_store.HasUser("old2"));
        }

        [Fact]
        public void ReplaceUsers_AddNewUsers()
        {
            _store.ReplaceUsers(new System.Collections.Generic.List<string> { "alice", "@bob", "+charlie" });
            Assert.True(_store.HasUser("alice"));
            Assert.True(_store.HasUser("bob"));
            Assert.True(_store.HasUser("charlie"));
        }

        [Fact]
        public void ReplaceUsers_AlsoUpdatesRawUsers()
        {
            _store.ReplaceUsers(new System.Collections.Generic.List<string> { "alice", "@bob" });
            Assert.Contains("alice", _store.RawUsers);
            Assert.Contains("bob", _store.RawUsers);
        }

        [Fact]
        public void ReplaceUsers_EmptyList_ClearsAllUsers()
        {
            _store.AddUser("alice");
            _store.ReplaceUsers(new System.Collections.Generic.List<string>());
            Assert.Empty(_store.Users);
        }

        // ── GetPrefix ─────────────────────────────────────────────────────────────

        [Fact]
        public void GetPrefix_OpUser_ReturnsAtSign()
        {
            _store.AddUser("@opuser");
            Assert.Equal("@", _store.GetPrefix("opuser"));
        }

        [Fact]
        public void GetPrefix_PlainUser_ReturnsEmpty()
        {
            _store.AddUser("plainuser");
            Assert.Equal("", _store.GetPrefix("plainuser"));
        }

        [Fact]
        public void GetPrefix_NonExistentUser_ReturnsEmpty()
        {
            Assert.Equal("", _store.GetPrefix("nobody"));
        }

        // ── ClearUsers ────────────────────────────────────────────────────────────

        [Fact]
        public void ClearUsers_RemovesAllUsers()
        {
            _store.AddUser("alice");
            _store.AddUser("bob");
            _store.ClearUsers();
            Assert.Empty(_store.Users);
            Assert.Empty(_store.RawUsers);
        }

        // ── Topic ─────────────────────────────────────────────────────────────────

        [Fact]
        public void Topic_DefaultIsEmpty()
        {
            Assert.Equal("", _store.Topic);
        }

        [Fact]
        public void Topic_CanBeSetDirectly()
        {
            _store.Topic = "Welcome to the channel!";
            Assert.Equal("Welcome to the channel!", _store.Topic);
        }
    }
}
