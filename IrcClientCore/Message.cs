using System;

namespace IrcClientCore
{
    public class Message
    {
        public Message()
        {
            DateTime date = DateTime.Now;
            Timestamp = date.ToString("HH:mm");
        }

        public string Timestamp {
            get
            {
                if (_date == null)
                {
                    var date = DateTime.Now;
                    _date = date.ToString("HH:mm");
                }
                return _date;
            }
            set => _date = value;
        }

        private string _date;

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

        public string Text { get; set; }
        public bool Mention { get; internal set; }
        public MessageType Type { get; set; }
    }

    public enum MessageType
    {
        Normal, Action, Info
    }
}