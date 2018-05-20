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

        public ChannelStore Store { get; private set; }
        public ObservableCollection<Message> Buffers { get; private set; }

        public bool ServerLog => Name == "Server";

        public Channel()
        {
            Store = new ChannelStore(this);
            Buffers = new ObservableCollection<Message>();
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