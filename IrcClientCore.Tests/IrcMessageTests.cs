using System;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for IRC message parsing compliance with the Modern IRC spec
    /// https://modern.ircdocs.horse/#client-to-server-protocol-structure
    /// </summary>
    public class IrcMessageTests
    {
        // ── Prefix Parsing ────────────────────────────────────────────────────────

        [Fact]
        public void Parse_FullUserPrefix_ExtractsNickUsernameHostname()
        {
            var msg = new IrcMessage(":nick!user@host PRIVMSG #chan :hello");
            Assert.True(msg.PrefixMessage.IsPrefixed);
            Assert.True(msg.PrefixMessage.IsUser);
            Assert.Equal("nick", msg.PrefixMessage.Nickname);
            Assert.Equal("user", msg.PrefixMessage.Username);
            Assert.Equal("host", msg.PrefixMessage.Hostname);
        }

        [Fact]
        public void Parse_ServerPrefix_NoUserParsed()
        {
            var msg = new IrcMessage(":irc.example.com 001 nick :Welcome");
            Assert.True(msg.PrefixMessage.IsPrefixed);
            Assert.False(msg.PrefixMessage.IsUser);
            Assert.Equal("irc.example.com", msg.PrefixMessage.Prefix);
        }

        [Fact]
        public void Parse_NickHostPrefix_TwoParts_SetsUserFlag()
        {
            // Some servers send :nick@host without username
            var msg = new IrcMessage(":nick@host QUIT :gone");
            Assert.True(msg.PrefixMessage.IsUser);
            Assert.Equal("nick", msg.PrefixMessage.Nickname);
            Assert.Equal("host", msg.PrefixMessage.Hostname);
        }

        [Fact]
        public void Parse_NoPrefix_IsPrefixedIsFalse()
        {
            var msg = new IrcMessage("PING :irc.server.com");
            Assert.False(msg.PrefixMessage.IsPrefixed);
        }

        // ── Command Parsing ───────────────────────────────────────────────────────

        [Fact]
        public void Parse_Command_IsExtracted()
        {
            var msg = new IrcMessage(":server PRIVMSG #chan :hi");
            Assert.Equal("PRIVMSG", msg.CommandMessage.Command);
        }

        [Fact]
        public void Parse_NumericCommand_IsExtracted()
        {
            var msg = new IrcMessage(":server 001 nick :Welcome to the server");
            Assert.Equal("001", msg.CommandMessage.Command);
        }

        [Fact]
        public void Parse_Parameters_AreExtracted()
        {
            // 353 has nick, channel-type, channel as params
            var msg = new IrcMessage(":server 353 nick = #channel :@op user");
            Assert.NotNull(msg.CommandMessage.Parameters);
            Assert.Equal(3, msg.CommandMessage.Parameters.Count);
            Assert.Equal("nick", msg.CommandMessage.Parameters[0]);
            Assert.Equal("=", msg.CommandMessage.Parameters[1]);
            Assert.Equal("#channel", msg.CommandMessage.Parameters[2]);
        }

        [Fact]
        public void Parse_NoParameters_ParametersIsNull()
        {
            var msg = new IrcMessage(":nick!user@host JOIN :#channel");
            // The only param would be the channel, but it's in trail
            Assert.Null(msg.CommandMessage.Parameters);
        }

        // ── Trailing Content ──────────────────────────────────────────────────────

        [Fact]
        public void Parse_TrailingContent_IsExtracted()
        {
            var msg = new IrcMessage(":nick!user@host PRIVMSG #chan :Hello World");
            Assert.True(msg.TrailMessage.HasTrail);
            Assert.Equal("Hello World", msg.TrailMessage.TrailingContent);
        }

        [Fact]
        public void Parse_TrailingContent_WithSpaces_PreservesSpaces()
        {
            var msg = new IrcMessage(":server 372 nick :- This is the MOTD with spaces");
            Assert.Equal("- This is the MOTD with spaces", msg.TrailMessage.TrailingContent);
        }

        [Fact]
        public void Parse_NoTrail_HasTrailIsFalse()
        {
            var msg = new IrcMessage(":nick!user@host JOIN #channel");
            Assert.False(msg.TrailMessage.HasTrail);
            Assert.Equal(string.Empty, msg.TrailMessage.TrailingContent);
        }

        // ── IRCv3 Message Tags ────────────────────────────────────────────────────

        [Fact]
        public void Parse_SingleTag_WithValue_AddedToMetadata()
        {
            var msg = new IrcMessage("@time=2023-01-01T00:00:00.000Z :nick!user@host PRIVMSG #chan :hi");
            Assert.True(msg.Metadata.ContainsKey("time"));
            Assert.Equal("2023-01-01T00:00:00.000Z", msg.Metadata["time"]);
        }

        [Fact]
        public void Parse_MultipleTags_AllAddedToMetadata()
        {
            var msg = new IrcMessage("@time=2023-01-01T00:00:00.000Z;msgid=abc123 :nick!user@host PRIVMSG #chan :hi");
            Assert.Equal("2023-01-01T00:00:00.000Z", msg.Metadata["time"]);
            Assert.Equal("abc123", msg.Metadata["msgid"]);
        }

        [Fact]
        public void Parse_ValuelessTag_StoredAsEmptyString()
        {
            var msg = new IrcMessage("@bot :nick!user@host PRIVMSG #chan :hi");
            Assert.True(msg.Metadata.ContainsKey("bot"));
            Assert.Equal("", msg.Metadata["bot"]);
        }

        [Fact]
        public void Parse_ClientTag_WithPlusPrefix()
        {
            var msg = new IrcMessage("@+draft/typing=active :nick!user@host TAGMSG #chan");
            Assert.True(msg.Metadata.ContainsKey("+draft/typing"));
            Assert.Equal("active", msg.Metadata["+draft/typing"]);
        }

        // ── Tag Value Unescaping (IRCv3 spec) ────────────────────────────────────

        [Fact]
        public void UnescapeTagValue_ColonEscape_ProducesSemicolon()
        {
            var msg = new IrcMessage("@key=a\\:b :server NOTICE * :test");
            Assert.Equal("a;b", msg.Metadata["key"]);
        }

        [Fact]
        public void UnescapeTagValue_SpaceEscape_ProducesSpace()
        {
            var msg = new IrcMessage("@key=hello\\sworld :server NOTICE * :test");
            Assert.Equal("hello world", msg.Metadata["key"]);
        }

        [Fact]
        public void UnescapeTagValue_BackslashEscape_ProducesBackslash()
        {
            var msg = new IrcMessage(@"@key=a\\b :server NOTICE * :test");
            Assert.Equal("a\\b", msg.Metadata["key"]);
        }

        [Fact]
        public void UnescapeTagValue_NewlineEscape_ProducesNewline()
        {
            var msg = new IrcMessage("@key=line1\\nline2 :server NOTICE * :test");
            Assert.Equal("line1\nline2", msg.Metadata["key"]);
        }

        [Fact]
        public void UnescapeTagValue_CarriageReturnEscape_ProducesCR()
        {
            var msg = new IrcMessage("@key=line1\\rline2 :server NOTICE * :test");
            Assert.Equal("line1\rline2", msg.Metadata["key"]);
        }

        [Fact]
        public void UnescapeTagValue_UnknownEscape_PassesThroughCharacter()
        {
            // Per IRCv3 spec: unknown escape sequences just pass through the escaped char
            var msg = new IrcMessage("@key=\\a :server NOTICE * :test");
            Assert.Equal("a", msg.Metadata["key"]);
        }

        [Fact]
        public void UnescapeTagValue_NoEscapes_ReturnsOriginal()
        {
            var msg = new IrcMessage("@key=plainvalue :server NOTICE * :test");
            Assert.Equal("plainvalue", msg.Metadata["key"]);
        }

        // ── Formatting Code Preservation ─────────────────────────────────────────

        [Fact]
        public void Parse_MircColorCodes_ArePreserved()
        {
            // \u0003 is the MIRC color code. Formatting codes are now preserved in TrailingContent
            // and parsed by IrcFormatParser instead of being stripped.
            var coloredMsg = "\u000304Red\u0003 Normal";
            var msg = new IrcMessage($":nick!user@host PRIVMSG #chan :{coloredMsg}");
            Assert.Equal(coloredMsg, msg.TrailMessage.TrailingContent);
        }

        [Fact]
        public void Parse_BoldControlCode_IsPreserved()
        {
            // \u0002 is bold. Formatting codes are now preserved in TrailingContent.
            var boldMsg = "\u0002Bold\u0002 Normal";
            var msg = new IrcMessage($":nick!user@host PRIVMSG #chan :{boldMsg}");
            Assert.Equal(boldMsg, msg.TrailMessage.TrailingContent);
        }

        // ── IrcMessage Constants ──────────────────────────────────────────────────

        [Fact]
        public void Constants_HaveCorrectValues()
        {
            Assert.Equal("msgid", IrcMessage.Id);
            Assert.Equal("time", IrcMessage.Time);
            Assert.Equal("+draft/reply", IrcMessage.Reply);
            Assert.Equal("+draft/typing", IrcMessage.Typing);
            Assert.Equal("+draft/react", IrcMessage.React);
        }

        // ── Original Message Preservation ────────────────────────────────────────

        [Fact]
        public void Parse_OriginalMessage_StoredWithoutTags()
        {
            // Tags are stripped; original message stored without them
            var msg = new IrcMessage("@time=2023-01-01T00:00:00.000Z :server NOTICE * :test");
            Assert.StartsWith(":", msg.OriginalMessage);
            Assert.DoesNotContain("@time", msg.OriginalMessage);
        }

        // ── Extended JOIN format ──────────────────────────────────────────────────

        [Fact]
        public void Parse_ExtendedJoin_ParsesParametersAndTrail()
        {
            // IRCv3 extended-join: :nick!user@host JOIN #channel accountname :Real Name
            var msg = new IrcMessage(":nick!user@host JOIN #channel accountname :Real Name");
            Assert.Equal("JOIN", msg.CommandMessage.Command);
            Assert.Equal("#channel", msg.CommandMessage.Parameters[0]);
            Assert.Equal("accountname", msg.CommandMessage.Parameters[1]);
            Assert.Equal("Real Name", msg.TrailMessage.TrailingContent);
        }
    }
}
