using System;
using System.Linq;
using System.Threading.Tasks;
using IrcClientCore.Handlers.BuiltIn;
using IrcClientCore.Tests.Helpers;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for IRCv3 handlers: CHGHOST, ACCOUNT, SETNAME, TAGMSG, BATCH, MONITOR.
    /// </summary>
    public class IrcV3HandlerTests : IAsyncLifetime
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

            // Pre-populate a channel with users for multi-handler tests
            await _irc.AddChannel("#channel");
            await _irc.SimulateReceive(":server 353 testuser = #channel :othernick");
            await _irc.SimulateReceive(":server 366 testuser #channel :End of /NAMES list.");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── CHGHOST (IRCv3.2) ─────────────────────────────────────────────────────

        [Fact]
        public async Task Chghost_UserInChannel_PostsInfoMessage()
        {
            await _irc.SimulateReceive(":othernick!user@oldhost CHGHOST newuser newhost");
            var buf = _irc.MockBuffers["#channel"];
            Assert.Contains(buf.Messages, m => m.Type == MessageType.Info && m.Text.Contains("newuser@newhost"));
        }

        [Fact]
        public async Task Chghost_FiresOnHostChangedEvent()
        {
            string? capturedNick = null;
            string? capturedOldHost = null;
            string? capturedNewHost = null;

            Action<string, string, string> handler = (nick, old, newHost) =>
            {
                capturedNick = nick;
                capturedOldHost = old;
                capturedNewHost = newHost;
            };
            ChgHostHandler.OnHostChanged += handler;

            try
            {
                await _irc.SimulateReceive(":othernick!user@oldhost CHGHOST newuser newhost");
                Assert.Equal("othernick", capturedNick);
                Assert.Equal("newhost", capturedNewHost);
            }
            finally
            {
                ChgHostHandler.OnHostChanged -= handler;
            }
        }

        [Fact]
        public async Task Chghost_UserNotInChannel_NoMessagePosted()
        {
            // User "stranger" is not in #channel
            await _irc.SimulateReceive(":stranger!user@oldhost CHGHOST newuser newhost");
            var buf = _irc.MockBuffers["#channel"];
            Assert.DoesNotContain(buf.Messages, m => m.Type == MessageType.Info && m.Text.Contains("newuser@newhost"));
        }

        // ── ACCOUNT (IRCv3.2 account-notify) ─────────────────────────────────────

        [Fact]
        public async Task Account_WithAccountName_SetsUserAccount()
        {
            await _irc.SimulateReceive(":othernick!user@host ACCOUNT :myaccount");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "othernick");
            Assert.Equal("myaccount", user?.Account);
        }

        [Fact]
        public async Task Account_WithAsterisk_ClearsUserAccount()
        {
            // First set an account
            await _irc.SimulateReceive(":othernick!user@host ACCOUNT :myaccount");
            // Then log out ("*" means no account)
            await _irc.SimulateReceive(":othernick!user@host ACCOUNT :*");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "othernick");
            Assert.Null(user?.Account);
        }

        [Fact]
        public async Task Account_FiresOnAccountChangedEvent()
        {
            string? capturedNick = null;
            string? capturedAccount = null;

            Action<string, string?> handler = (nick, acct) =>
            {
                capturedNick = nick;
                capturedAccount = acct;
            };
            AccountNotifyHandler.OnAccountChanged += handler;

            try
            {
                await _irc.SimulateReceive(":othernick!user@host ACCOUNT :testaccount");
                Assert.Equal("othernick", capturedNick);
                Assert.Equal("testaccount", capturedAccount);
            }
            finally
            {
                AccountNotifyHandler.OnAccountChanged -= handler;
            }
        }

        [Fact]
        public async Task Account_Asterisk_FiresOnAccountChangedWithNull()
        {
            string? capturedAccount = "initial";

            Action<string, string?> handler = (nick, acct) => capturedAccount = acct;
            AccountNotifyHandler.OnAccountChanged += handler;

            try
            {
                await _irc.SimulateReceive(":othernick!user@host ACCOUNT :*");
                Assert.Null(capturedAccount);
            }
            finally
            {
                AccountNotifyHandler.OnAccountChanged -= handler;
            }
        }

        [Fact]
        public async Task Account_UpdatesAcrossAllChannels()
        {
            await _irc.AddChannel("#channel2");
            await _irc.SimulateReceive(":server 353 testuser = #channel2 :othernick");
            await _irc.SimulateReceive(":server 366 testuser #channel2 :End of /NAMES list.");

            await _irc.SimulateReceive(":othernick!user@host ACCOUNT :sharedaccount");

            foreach (var chanName in new[] { "#channel", "#channel2" })
            {
                var users = _irc.GetChannelUsers(chanName);
                var user = users.FirstOrDefault(u => u.Nick == "othernick");
                Assert.Equal("sharedaccount", user?.Account);
            }
        }

        [Fact]
        public async Task AccountTag_OnJoin_FiresOnAccountChangedEvent()
        {
            string? capturedNick = null;
            string? capturedAccount = null;

            Action<string, string?> handler = (nick, acct) =>
            {
                capturedNick = nick;
                capturedAccount = acct;
            };
            AccountNotifyHandler.OnAccountChanged += handler;

            try
            {
                await _irc.SimulateReceive("@account=joinedacct :othernick!user@host JOIN :#channel");
                Assert.Equal("othernick", capturedNick);
                Assert.Equal("joinedacct", capturedAccount);
            }
            finally
            {
                AccountNotifyHandler.OnAccountChanged -= handler;
            }
        }

        // ── SETNAME (IRCv3.2) ─────────────────────────────────────────────────────

        [Fact]
        public async Task Setname_UpdatesUserRealName()
        {
            await _irc.SimulateReceive(":othernick!user@host SETNAME :New Real Name");

            var users = _irc.GetChannelUsers("#channel");
            var user = users.FirstOrDefault(u => u.Nick == "othernick");
            Assert.Equal("New Real Name", user?.RealName);
        }

        [Fact]
        public async Task Setname_PostsInfoMessageToChannel()
        {
            await _irc.SimulateReceive(":othernick!user@host SETNAME :Updated Name");
            var buf = _irc.MockBuffers["#channel"];
            Assert.Contains(buf.Messages, m => m.Type == MessageType.Info && m.Text.Contains("Updated Name"));
        }

        [Fact]
        public async Task Setname_FiresOnNameChangedEvent()
        {
            string? capturedNick = null;
            string? capturedName = null;

            Action<string, string> handler = (nick, name) =>
            {
                capturedNick = nick;
                capturedName = name;
            };
            SetNameHandler.OnNameChanged += handler;

            try
            {
                await _irc.SimulateReceive(":othernick!user@host SETNAME :My New Name");
                Assert.Equal("othernick", capturedNick);
                Assert.Equal("My New Name", capturedName);
            }
            finally
            {
                SetNameHandler.OnNameChanged -= handler;
            }
        }

        // ── TAGMSG (IRCv3 message-tags / typing indicators) ───────────────────────

        [Fact]
        public async Task Tagmsg_TypingActive_PostsTypingMessage()
        {
            await _irc.SimulateReceive("@+draft/typing=active :othernick!user@host TAGMSG #channel");

            var buf = _irc.MockBuffers["#channel"];
            Assert.Contains(buf.Messages, m => m.Text != null && m.Text.Contains("typing") && m.Text.Contains("othernick"));
        }

        [Fact]
        public async Task Tagmsg_TypingDone_PostsTypingDoneMessage()
        {
            await _irc.SimulateReceive("@+draft/typing=done :othernick!user@host TAGMSG #channel");
            var buf = _irc.MockBuffers["#channel"];
            Assert.Contains(buf.Messages, m => m.Text != null && m.Text.Contains("done"));
        }

        [Fact]
        public async Task Tagmsg_Reaction_PostsReactionMessage()
        {
            await _irc.SimulateReceive("@+draft/react=👍 :othernick!user@host TAGMSG #channel");
            var buf = _irc.MockBuffers["#channel"];
            Assert.Contains(buf.Messages, m => m.Text != null && m.Text.Contains("reacted") && m.Text.Contains("👍"));
        }

        [Fact]
        public async Task Tagmsg_NoRelevantTags_NoMessagePosted()
        {
            _irc.MockBuffers.TryGetValue("#channel", out var chanBuf);
            var countBefore = chanBuf?.Messages.Count ?? 0;
            await _irc.SimulateReceive(":othernick!user@host TAGMSG #channel");
            var countAfter = chanBuf?.Messages.Count ?? 0;
            Assert.Equal(countBefore, countAfter);
        }

        // ── BATCH (IRCv3.2) ───────────────────────────────────────────────────────

        [Fact]
        public async Task Batch_Start_FiresOnBatchStartEvent()
        {
            BatchInfo? capturedBatch = null;
            Action<BatchInfo> handler = b => capturedBatch = b;
            BatchHandler.OnBatchStart += handler;

            try
            {
                await _irc.SimulateReceive(":server BATCH +ref123 labeled-response");
                Assert.NotNull(capturedBatch);
                Assert.Equal("ref123", capturedBatch!.Reference);
                Assert.Equal("labeled-response", capturedBatch.Type);
            }
            finally
            {
                BatchHandler.OnBatchStart -= handler;
            }
        }

        [Fact]
        public async Task Batch_End_FiresOnBatchEndEvent()
        {
            string? capturedRef = null;
            Action<string> handler = r => capturedRef = r;
            BatchHandler.OnBatchEnd += handler;

            try
            {
                await _irc.SimulateReceive(":server BATCH +endtest netjoin");
                await _irc.SimulateReceive(":server BATCH -endtest");
                Assert.Equal("endtest", capturedRef);
            }
            finally
            {
                BatchHandler.OnBatchEnd -= handler;
            }
        }

        [Fact]
        public async Task Batch_GetBatch_ReturnsActiveBatchInfo()
        {
            // After BATCH +ref, the batch is accessible via GetBatch()
            await _irc.SimulateReceive(":server BATCH +getbatchref labeled-response");

            var batchHandler = _irc.HandlerManager.GetHandler<BatchHandler>();
            var batch = batchHandler!.GetBatch("getbatchref");
            Assert.NotNull(batch);
            Assert.Equal("getbatchref", batch!.Reference);
            Assert.Equal("labeled-response", batch.Type);
        }

        [Fact]
        public async Task Batch_MessageWithBatchTag_StillProcessedByCommandHandler()
        {
            // Batch handler returns true so other handlers (PRIVMSG) still run
            await _irc.AddChannel("#channel");
            await _irc.SimulateReceive(":server BATCH +batchref netjoin");
            await _irc.SimulateReceive("@batch=batchref :nick!user@host PRIVMSG #channel :batched message");

            var buf = _irc.MockBuffers["#channel"];
            Assert.Contains(buf.Messages, m => m.Text == "batched message");
        }

        [Fact]
        public async Task Batch_MessageWithBatchTag_FiresOnBatchMessageEvent()
        {
            string? capturedRef = null;
            IrcMessage? capturedMessage = null;
            Action<string, IrcMessage> handler = (batchRef, msg) =>
            {
                capturedRef = batchRef;
                capturedMessage = msg;
            };
            BatchHandler.OnBatchMessage += handler;

            try
            {
                await _irc.SimulateReceive(":server BATCH +batchref labeled-response");
                await _irc.SimulateReceive("@batch=batchref :nick!user@host PRIVMSG #channel :batched payload");

                Assert.Equal("batchref", capturedRef);
                Assert.NotNull(capturedMessage);
                Assert.Equal("PRIVMSG", capturedMessage!.CommandMessage.Command);
            }
            finally
            {
                BatchHandler.OnBatchMessage -= handler;
            }
        }

        // ── MONITOR (IRCv3.2) ─────────────────────────────────────────────────────

        [Fact]
        public async Task Monitor_730_UserOnline_FiresEvent()
        {
            string? capturedNick = null;
            Action<string> handler = nick => capturedNick = nick;
            MonitorHandler.OnUserOnline += handler;

            try
            {
                await _irc.SimulateReceive(":server 730 testuser :watcheduser!user@host");
                Assert.Equal("watcheduser", capturedNick);
            }
            finally
            {
                MonitorHandler.OnUserOnline -= handler;
            }
        }

        [Fact]
        public async Task Monitor_730_MultipleNicks_CommaSeparated_AllFired()
        {
            var onlineNicks = new System.Collections.Generic.List<string>();
            Action<string> handler = nick => onlineNicks.Add(nick);
            MonitorHandler.OnUserOnline += handler;

            try
            {
                await _irc.SimulateReceive(":server 730 testuser :alpha!a@host,beta!b@host,gamma!c@host");
                Assert.Contains("alpha", onlineNicks);
                Assert.Contains("beta", onlineNicks);
                Assert.Contains("gamma", onlineNicks);
                Assert.Equal(3, onlineNicks.Count);
            }
            finally
            {
                MonitorHandler.OnUserOnline -= handler;
            }
        }

        [Fact]
        public async Task Monitor_731_UserOffline_FiresEvent()
        {
            string? capturedNick = null;
            Action<string> handler = nick => capturedNick = nick;
            MonitorHandler.OnUserOffline += handler;

            try
            {
                await _irc.SimulateReceive(":server 731 testuser :watcheduser");
                Assert.Equal("watcheduser", capturedNick);
            }
            finally
            {
                MonitorHandler.OnUserOffline -= handler;
            }
        }

        [Fact]
        public async Task Monitor_734_ListFull_PostsError()
        {
            // Should not throw, should post a message
            await _irc.SimulateReceive(":server 734 testuser watcheduser :Monitor list is full");
        }
    }
}
