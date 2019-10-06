using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public class ChannelsGroup : ObservableCollection<Channel>
    {
        public ChannelsGroup(IEnumerable<Channel> items, Irc irc) : base(items)
        {
            this.Irc = irc;
        }

        private bool _serverAdded;
        public Channel ServerLog { get; private set; }
        public string Server { get; set; }
        public Irc Irc { get; }

        public bool Contains(string channel)
        {
            if (channel == null)
            {
                return false;
            }
            return this.Any(chan =>
            {
                if (chan.Name != null)
                {
                    return chan.NameLower == channel.ToLower();
                }

                return channel == "";
            });
        }

        public void Insert(int position, string channel)
        {
            if (channel == "Server" || channel == null && !_serverAdded)
            {
                ServerLog = new Channel(Irc, channel)
                {
                    Server = Server
                };

                _serverAdded = true;
                return;
            }

            this.Insert(position, new Channel(Irc, channel)
            {
                Server = Server
            });
        }

        public void Remove(string channel)
        {
            this.Remove(Get(channel));
        }

        public Channel this[string channel]
        {
            get 
            {
                return Get(channel);
            }
        }

        public Channel Get(string channel)
        {
            if (channel == "" || !Contains(channel))
            {
                return ServerLog;
            }

            return this.FirstOrDefault(chan => chan.NameLower == channel.ToLower());
        }
    }
}
