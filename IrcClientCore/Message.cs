using System;
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
        public bool Mention { get; internal set; }
        public MessageType Type { get; set; }

        public int MessageHash => GetHashCode();
    }

    public enum MessageType
    {
        Normal, Action, Info, JoinPart
    }
}