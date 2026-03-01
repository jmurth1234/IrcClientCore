using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class KickHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            // handle messages to this irc client
            var destination = parsedLine.CommandMessage.Parameters[0];
            var reciever = parsedLine.CommandMessage.Parameters[1];
            var content = parsedLine.TrailMessage.TrailingContent;
            if (!Irc.ChannelList.Contains(destination))
            {
                await Irc.AddChannel(destination);
            }

            var msg = new Message();

            msg.Type = MessageType.Info;

            var contentFormatted = IrcFormatParser.Parse(content);

            if (reciever == Irc.Server.Username)
            {
                msg.User = parsedLine.PrefixMessage.Nickname;
                msg.Text = "kicked you from the channel: " + contentFormatted.PlainText;
                msg.FormattedText = IrcFormatParser.Parse("kicked you from the channel: " + content);
            }
            else
            {
                msg.User = parsedLine.PrefixMessage.Nickname;
                msg.Text = string.Format("kicked {0} from the channel: {1}", reciever, contentFormatted.PlainText);
                msg.FormattedText = IrcFormatParser.Parse(string.Format("kicked {0} from the channel: {1}", reciever, content));
            }

            Irc.AddMessage(destination, msg);

            return true;
        }
    }
}
