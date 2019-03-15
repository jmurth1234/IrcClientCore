﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class PrivmsgHandler : BaseHandler
    {
        public MessageType Type = MessageType.Normal;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            // handle messages to this irc client
            var destination = parsedLine.CommandMessage.Parameters[0];
            var content = parsedLine.TrailMessage.TrailingContent;

            if (destination == Irc.Server.Username)
            {
                destination = parsedLine.PrefixMessage.Nickname;
            }

            if (!Irc.ChannelList.Contains(destination))
            {
                await Irc.AddChannel(destination);
            }

            var msg = new Message();
            msg.Channel = destination;
            msg.Type = Type;
            msg.User = parsedLine.PrefixMessage.Nickname;
            if (parsedLine.ServerTime != null)
            {
                var time = DateTime.Parse(parsedLine.ServerTime);
                msg.Date = time;
            }

            if (content.Contains("ACTION"))
            {
                msg.Text = content.Replace("ACTION ", "");
                msg.Type = MessageType.Action;
            }
            else
            {
                msg.Text = content;
            }

            if ((parsedLine.TrailMessage.TrailingContent.Contains(Irc.Server.Username) || parsedLine.CommandMessage.Parameters[0] == Irc.Server.Username))
            {
                msg.Mention = true;
                Irc.AddMention(msg);
            }

            if (destination == "*")
            {
                Irc.ChannelList.ServerLog?.Buffers.Add(msg);
            }
            else
            {
                Irc.AddMessage(destination, msg);
            }
            return true;
        }
    }
}
