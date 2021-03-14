using System.Collections.ObjectModel;
using System.Linq;

namespace IrcClientCore
{
    public class Buffer : IBuffer
    {
        public Buffer()
        {
            this.Collection = new ObservableCollection<Message>();
        }

        public ObservableCollection<Message> Collection { get; set; }

        public void Add(Message msg)
        {
            Collection.Add(msg);
        }

        public Message GetMessage(string msgId)
        {
            return Collection.FirstOrDefault(m => m.MessageId == msgId);
        }
        
        public void UpdateMessage(Message message)
        {
            // stub method
        }
    }
}