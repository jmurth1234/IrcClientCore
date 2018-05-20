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
        public ChannelsGroup(IEnumerable<Channel> items) : base(items) { }

        private bool _serverAdded;

        public Channel ServerLog { get; private set; }

        public bool Contains(string s)
        {
            return this.Any(chan => chan.Name.ToLower() == s.ToLower() );
        }

        public void Insert(int i, string s)
        {
            if (s == "Server" && !_serverAdded)
            {
                ServerLog = new Channel
                {
                    Name = s,
                    Server = Server
                };

                _serverAdded = true;
                return;
            }

            this.Insert(i, new Channel
            {
                Name = s,
                Server = Server
            });
        }

        public void Remove(string s)
        {
            this.Remove(Get(s));
        }

        public Channel this[string s]
        {
            get { return Get(s); }
        }

        public Channel Get(string s)
        {
            return this.FirstOrDefault(chan => chan.Name.ToLower() == s.ToLower());
        }

        public string Server { get; set; }
    }
}
