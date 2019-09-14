using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    class NamesHandler : BaseHandler
    {
        private Dictionary<string, List<string>> _namesTable = new Dictionary<string, List<string>>();

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            if (parsedLine.CommandMessage.Command == "353")
            {
                var channel = parsedLine.CommandMessage.Parameters[2];

                var list = parsedLine.TrailMessage.TrailingContent.Split(' ').ToList();
                if (!_namesTable.ContainsKey(channel))
                {
                    _namesTable.Add(channel, list);
                }
                else
                {
                    _namesTable[channel].AddRange(list);
                }
            }
            else if (parsedLine.CommandMessage.Command == "366")
            {
                var channel = parsedLine.CommandMessage.Parameters[1];

                if (!Irc.ChannelList.Contains(channel))
                {
                    await Irc.AddChannel(channel);
                }

                Irc.ChannelList[channel].Store.ReplaceUsers(_namesTable[channel]);

                _namesTable.Remove(channel);
            }
            return true;
        }
    }
}
