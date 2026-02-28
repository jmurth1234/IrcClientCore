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
                // Format: #channel :nick1=+~user@host.com @nick2=user2@host2.com
                // or with account: nick1!~user@host.com (accountname)
                if (CapHandler.SupportsUserHostInNames)
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
        /// Format: nick1=+~user@host.com or @nick2=user2@host2.com
        /// </summary>
        private List<string> ParseUserHostNames(List<string> names)
        {
            var result = new List<string>();

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // Format: nick=+~user@host.com or @nick=user@host.com
                // We need to extract just the nick for the user list

                string nick = name;

                // Remove prefix characters (~&@%+) from the beginning
                nick = RemovePrefix(nick);

                // If there's a = or @ after the nick, extract just the nick part
                if (nick.Contains("="))
                {
                    nick = nick.Split('=')[0];
                }
                else if (nick.Contains("@"))
                {
                    nick = nick.Split('@')[0];
                }

                if (!string.IsNullOrEmpty(nick))
                {
                    // Re-add prefix if it was stripped
                    var prefix = GetPrefix(name);
                    result.Add(prefix + nick);
                }
            }

            return result;
        }

        private string GetPrefix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            var firstChar = name[0].ToString();
            var prefixChars = new string[] { "~", "&", "@", "%", "+" };
            if (prefixChars.Contains(firstChar))
            {
                return firstChar;
            }
            return "";
        }

        private string RemovePrefix(string nick)
        {
            var prefixChars = new char[] { '~', '&', '@', '%', '+' };
            foreach (var c in prefixChars)
            {
                if (nick.StartsWith(c.ToString()))
                {
                    return nick.Substring(1);
                }
            }
            return nick;
        }
    }
}
