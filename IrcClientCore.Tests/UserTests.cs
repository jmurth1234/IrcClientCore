using System;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for the User model: nick/prefix extraction, sorting, and IRCv3 fields.
    /// https://modern.ircdocs.horse/#membership-prefixes
    /// </summary>
    public class UserTests
    {
        // ── Nick extraction ───────────────────────────────────────────────────────

        [Fact]
        public void Nick_PlainUsername_ReturnedAsIs()
        {
            var user = new User { FullUsername = "plainuser" };
            Assert.Equal("plainuser", user.Nick);
        }

        [Fact]
        public void Nick_OpPrefix_StrippedFromNick()
        {
            var user = new User { FullUsername = "@opuser" };
            Assert.Equal("opuser", user.Nick);
        }

        [Fact]
        public void Nick_VoicePrefix_StrippedFromNick()
        {
            var user = new User { FullUsername = "+voiceuser" };
            Assert.Equal("voiceuser", user.Nick);
        }

        [Fact]
        public void Nick_FounderPrefix_StrippedFromNick()
        {
            var user = new User { FullUsername = "~founderuser" };
            Assert.Equal("founderuser", user.Nick);
        }

        [Fact]
        public void Nick_ProtectedPrefix_StrippedFromNick()
        {
            var user = new User { FullUsername = "&protecteduser" };
            Assert.Equal("protecteduser", user.Nick);
        }

        [Fact]
        public void Nick_HalfopPrefix_StrippedFromNick()
        {
            var user = new User { FullUsername = "%halfopuser" };
            Assert.Equal("halfopuser", user.Nick);
        }

        [Fact]
        public void Nick_EmptyUsername_ReturnsEmpty()
        {
            var user = new User { FullUsername = "" };
            Assert.Equal("", user.Nick);
        }

        [Fact]
        public void Nick_NullUsername_ReturnsEmpty()
        {
            var user = new User { FullUsername = null! };
            Assert.Equal("", user.Nick);
        }

        // ── Prefix extraction ─────────────────────────────────────────────────────

        [Fact]
        public void Prefix_PlainUsername_ReturnsEmpty()
        {
            var user = new User { FullUsername = "plainuser" };
            Assert.Equal("", user.Prefix);
        }

        [Fact]
        public void Prefix_OpUsername_ReturnsAtSign()
        {
            var user = new User { FullUsername = "@opuser" };
            Assert.Equal("@", user.Prefix);
        }

        [Fact]
        public void Prefix_VoiceUsername_ReturnsPlus()
        {
            var user = new User { FullUsername = "+voiceuser" };
            Assert.Equal("+", user.Prefix);
        }

        [Fact]
        public void Prefix_FounderUsername_ReturnsTilde()
        {
            var user = new User { FullUsername = "~founderuser" };
            Assert.Equal("~", user.Prefix);
        }

        // ── Sorting (CompareTo) ───────────────────────────────────────────────────

        [Fact]
        public void CompareTo_FounderBeforeOp()
        {
            var founder = new User { FullUsername = "~alice" };
            var op = new User { FullUsername = "@bob" };
            Assert.True(founder.CompareTo(op) < 0, "Founder should sort before op");
        }

        [Fact]
        public void CompareTo_OpBeforeVoice()
        {
            var op = new User { FullUsername = "@alpha" };
            var voice = new User { FullUsername = "+beta" };
            Assert.True(op.CompareTo(voice) < 0, "Op should sort before voice");
        }

        [Fact]
        public void CompareTo_VoiceBeforePlain()
        {
            var voice = new User { FullUsername = "+voiced" };
            var plain = new User { FullUsername = "plain" };
            Assert.True(voice.CompareTo(plain) < 0, "Voiced should sort before plain");
        }

        [Fact]
        public void CompareTo_SamePrefix_AlphabeticalOrder()
        {
            var alice = new User { FullUsername = "alice" };
            var bob = new User { FullUsername = "bob" };
            Assert.True(alice.CompareTo(bob) < 0, "alice should sort before bob alphabetically");
        }

        [Fact]
        public void CompareTo_SamePrefix_SameName_ReturnsZero()
        {
            var a = new User { FullUsername = "@op" };
            var b = new User { FullUsername = "@op" };
            Assert.Equal(0, a.CompareTo(b));
        }

        [Fact]
        public void CompareTo_NullObject_ReturnsPositive()
        {
            var user = new User { FullUsername = "nick" };
            Assert.True(user.CompareTo(null) > 0);
        }

        [Fact]
        public void CompareTo_NonUserObject_ThrowsArgumentException()
        {
            var user = new User { FullUsername = "nick" };
            Assert.Throws<ArgumentException>(() => user.CompareTo("not a user"));
        }

        // ── IRCv3 fields ──────────────────────────────────────────────────────────

        [Fact]
        public void Account_DefaultIsNull()
        {
            var user = new User { FullUsername = "nick" };
            Assert.Null(user.Account);
        }

        [Fact]
        public void Account_CanBeSet()
        {
            var user = new User { FullUsername = "nick" };
            user.Account = "myaccount";
            Assert.Equal("myaccount", user.Account);
        }

        [Fact]
        public void RealName_CanBeSet()
        {
            var user = new User { FullUsername = "nick" };
            user.RealName = "John Doe";
            Assert.Equal("John Doe", user.RealName);
        }

        [Fact]
        public void IsAway_DefaultIsFalse()
        {
            var user = new User { FullUsername = "nick" };
            Assert.False(user.IsAway);
        }

        [Fact]
        public void AwayMessage_CanBeSet()
        {
            var user = new User { FullUsername = "nick" };
            user.IsAway = true;
            user.AwayMessage = "Be right back";
            Assert.Equal("Be right back", user.AwayMessage);
        }

        // ── Prefix map completeness ───────────────────────────────────────────────

        [Fact]
        public void PrefixMap_ContainsAllStandardPrefixes()
        {
            Assert.True(User.PrefixMap.ContainsKey("~")); // Founder
            Assert.True(User.PrefixMap.ContainsKey("&")); // Protected
            Assert.True(User.PrefixMap.ContainsKey("@")); // Op
            Assert.True(User.PrefixMap.ContainsKey("%")); // Halfop
            Assert.True(User.PrefixMap.ContainsKey("+")); // Voice
            Assert.True(User.PrefixMap.ContainsKey(""));  // None
        }

        [Fact]
        public void PrefixMap_FounderHasHighestPriority()
        {
            Assert.True(User.PrefixMap["~"] > User.PrefixMap["@"],
                "Founder (~) should have higher priority than op (@)");
        }

        [Fact]
        public void PrefixMap_OpHigherThanVoice()
        {
            Assert.True(User.PrefixMap["@"] > User.PrefixMap["+"],
                "Op (@) should have higher priority than voice (+)");
        }
    }
}
