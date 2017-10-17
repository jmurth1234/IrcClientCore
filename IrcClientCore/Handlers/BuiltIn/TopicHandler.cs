using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class TopicHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            // handle topic recieved
            var topic = parsedLine.TrailMessage.TrailingContent;
            var channelPos = (parsedLine.CommandMessage.Command == "332") ? 1 : 0;
            var channel = parsedLine.CommandMessage.Parameters[channelPos];

            if (!Irc.ChannelList.Contains(channel))
            {
                await Irc.AddChannel(channel);
            }

            Message msg = new Message();
            msg.Type = MessageType.Info;

            msg.User = "";
            msg.Text = String.Format("Topic for channel {0}: {1}", channel, topic);
            Irc.AddMessage(channel, msg);
            Irc.ChannelList[channel].Store.SetTopic(topic);
            return true;
        }
    }
}
