using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IrcClientCore
{
    public class Message
    {
        public Message()
        {
            Date = DateTime.Now;
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                _timestamp = value.ToString("HH:mm");
            }
        }

        public string Timestamp {
            get
            {
                if (_timestamp == null)
                {
                    var date = DateTime.Now;
                    _timestamp = date.ToString("HH:mm");
                }
                return _timestamp;
            }
        }

        private string _timestamp;

        public string User {
            get
            {
                if (_username == "" )
                {
                    return "*";
                }
                return _username;
            }
            set => _username = value;
        }

        public string Channel { get; set; }
        
        private string _username;
        private DateTime _date;

        public string Text { get; set; }
        public bool Mention { get; set; }
        public MessageType Type { get; set; }

        public int MessageHash => GetHashCode();

        public override bool Equals(object obj)
        {
            var message = obj as Message;
            return message != null &&
                   Date == message.Date &&
                   User == message.User &&
                   Channel == message.Channel &&
                   Text == message.Text &&
                   Type == message.Type;
        }

        public override int GetHashCode()
        {
            var hashCode = 1712867738;
            hashCode = hashCode * -1521134295 + Date.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(User);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Channel);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text);
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            return hashCode;
        }
    }

    public enum MessageType
    {
        Normal, Action, Info, JoinPart, Notice
    }
}