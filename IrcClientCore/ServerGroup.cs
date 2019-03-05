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
            return this.Any(chan => chan.Name.ToLower() == channel.ToLower() );
        }

        public void Insert(int position, string channel)
        {
            if (channel == "Server" && !_serverAdded)
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
            get { return Get(channel); }
        }

        public Channel Get(string channel)
        {
            return this.FirstOrDefault(chan => chan.Name.ToLower() == channel.ToLower());
        }
    }
}
