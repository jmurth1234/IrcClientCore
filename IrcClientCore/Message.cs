using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IrcClientCore
{
    public class Message : INotifyPropertyChanged
    {
        public Message()
        {
            Date = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public string MessageId { get; set; }

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
                if (_username == "")
                {
                    return "*";
                }
                return _username;
            }
            set => _username = value;
        }

        private int _replies;

        public int Replies
        {
            get => _replies;
            set 
            {
                _replies = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("HasReplies");
            }
        }

        public bool HasReplies => Replies > 0;

        public string ReplyTo { get; set; }

        public string Channel { get; set; }
        public string ChannelLowerCase => Channel.ToLower();
        
        private string _username = "";
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
        Normal, Action, Info, JoinPart, Notice, MOTD
    }
}