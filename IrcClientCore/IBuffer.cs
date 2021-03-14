namespace IrcClientCore
{
    public interface IBuffer
    {
        void Add(Message msg);
        Message GetMessage(string msgReplyTo);
        void UpdateMessage(Message message);
    }
}