using System.Collections.Generic;
using System.Linq;

namespace IrcClientCore.Tests.Helpers
{
    public class MockBuffer : IBuffer
    {
        public List<Message> Messages { get; } = new List<Message>();

        public void Add(Message msg) => Messages.Add(msg);

        public Message GetMessage(string msgReplyTo)
            => Messages.FirstOrDefault(m => m.MessageId == msgReplyTo);

        public void UpdateMessage(Message message)
        {
            var idx = Messages.FindIndex(m => m.MessageId == message.MessageId);
            if (idx >= 0) Messages[idx] = message;
        }
    }
}
