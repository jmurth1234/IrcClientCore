﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class NamesHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var list = parsedLine.TrailMessage.TrailingContent.Split(' ').ToList();
            var channel = parsedLine.CommandMessage.Parameters[2];

            if (!Irc.ChannelList.Contains(channel))
            {
                await Irc.AddChannel(channel);
            }

            Irc.ChannelList[channel].Store.AddUsers(list);

            return true;
        }
    }
}
