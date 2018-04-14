using System;

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
                if (Type == MessageType.Action || Type == MessageType.Info)
                {
                    return String.Format("* {0}", _username);
                }
                if (_username == "" )
                {
                    return "*";
                }
                return String.Format("<{0}>", _username);
            }
            set => _username = value;
        }

        private string _username;
        private DateTime _date;

        public string Text { get; set; }
        public bool Mention { get; internal set; }
        public MessageType Type { get; set; }
    }

    public enum MessageType
    {
        Normal, Action, Info
    }
}