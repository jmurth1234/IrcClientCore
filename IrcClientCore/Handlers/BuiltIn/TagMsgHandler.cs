using System;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles TAGMSG command (IRCv3 message-tags)
    /// TAGMSG is a message with tags but no text content, used for
    /// typing indicators, reactions, etc.
    /// Format: @+draft/typing=active :nick!user@host TAGMSG #channel
    /// </summary>
    class TagMsgHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            if (parsedLine.CommandMessage.Command != "TAGMSG")
            {
                return true;
            }

            var nick = parsedLine.PrefixMessage.Nickname;
            var target = parsedLine.CommandMessage.Parameters?.Count > 0
                ? parsedLine.CommandMessage.Parameters[0]
                : null;

            if (string.IsNullOrEmpty(target))
                return true;

            // Handle typing indicator
            if (parsedLine.Metadata.TryGetValue(IrcMessage.Typing, out var typingState))
            {
                Irc.ClientMessage(target, $"{nick} is typing ({typingState})");
            }

            // Handle reaction
            if (parsedLine.Metadata.TryGetValue(IrcMessage.React, out var reaction))
            {
                Irc.ClientMessage(target, $"{nick} reacted: {reaction}");
            }

            return true;
        }
    }
}
