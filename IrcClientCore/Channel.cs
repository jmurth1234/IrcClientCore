using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;

namespace IrcClientCore
{
    [DataContract]
    public class Channel
    {
        [DataMember]
        public string Server { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public bool HasUnread { get; set; }
        [DataMember]
        public bool HasMentions { get; set; }
        [DataMember]
        public int UnreadCount { get; set; }
        [DataMember]
        public bool CurrentlyViewing { 
            get 
            {
                return _viewing;
            }
            set 
            {
                this._viewing = value;
                if (value) 
                {
                    UnreadCount = 0;
                    HasMentions = false;
                    HasUnread = false;
                }
            }
        }

        private bool _viewing = false;

        public ChannelStore Store { get; private set; }
        public ObservableCollection<Message> Buffers { get; private set; }

        public bool ServerLog => Name == "Server";

        public Channel(Irc irc)
        {
            Store = new ChannelStore(this);
            Buffers = irc.CreateChannelBuffer();
        }

        public void ClientMessage(string text)
        {
            var msg = new Message();
            msg.User = "";
            msg.Type = MessageType.Info;
            msg.Text = text;

            Buffers.Add(msg);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
