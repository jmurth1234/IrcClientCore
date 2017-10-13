using System.Collections.ObjectModel;
using System.IO;

namespace IrcClientCore
{
    public class Channel
    {
        public string Server { get; set; }
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
            Message msg = new Message();
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