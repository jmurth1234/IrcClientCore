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

                // Handle userhost-in-names (IRCv3.3)
                // Format: [@+]nick!user@host
                var capHandler = Irc.HandlerManager.GetHandler<CapHandler>();
                if (capHandler != null && capHandler.SupportsUserHostInNames)
                {
                    list = ParseUserHostNames(list);
                }

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

        /// <summary>
        /// Parse userhost-in-names format into nicknames
        /// Format: [@+]nick!user@host
        /// </summary>
        private List<string> ParseUserHostNames(List<string> names)
        {
            var result = new List<string>();

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // Format: [@+%~&]nick!user@host
                // Strip prefix chars, then split on ! to get nick
                string entry = name;

                // Get and strip all leading prefix characters
                var prefix = GetPrefix(entry);
                string withoutPrefix = entry.Substring(prefix.Length);

                // Split on ! to separate nick from user@host
                string nick;
                int bangIndex = withoutPrefix.IndexOf('!');
                if (bangIndex >= 0)
                {
                    nick = withoutPrefix.Substring(0, bangIndex);
                }
                else
                {
                    nick = withoutPrefix;
                }

                if (!string.IsNullOrEmpty(nick))
                {
                    result.Add(prefix + nick);
                }
            }

            return result;
        }

        private string GetPrefix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            var prefixChars = new HashSet<char> { '~', '&', '@', '%', '+' };
            int i = 0;
            while (i < name.Length && prefixChars.Contains(name[i]))
            {
                i++;
            }
            return name.Substring(0, i);
        }

        private string RemovePrefix(string nick)
        {
            var prefixChars = new HashSet<char> { '~', '&', '@', '%', '+' };
            int i = 0;
            while (i < nick.Length && prefixChars.Contains(nick[i]))
            {
                i++;
            }
            return nick.Substring(i);
        }
    }
}
