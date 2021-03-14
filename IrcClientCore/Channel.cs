using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace IrcClientCore
{
    [DataContract]
    public class Channel : BaseObject
    {
        [DataMember]
        public string Server { get; set; }
        [DataMember]
        public string Name { get; set; }
        public string NameLower => Name.ToLower();
        [DataMember]
        public bool HasUnread
        {
            get => _hasUnread;
            set
            {
                _hasUnread = value;
                NotifyPropertyChanged();
            }
        }
        [DataMember]
        public bool HasMentions
        {
            get => _hasMentions;
            set
            {
                _hasMentions = value;
                NotifyPropertyChanged();
            }
        }
        [DataMember]
        public int UnreadCount {
            get => _unreadCount;
            set
            {
                _unreadCount = value;
                NotifyPropertyChanged();
            }
        }
        [DataMember]
        public bool CurrentlyViewing
        {
            get
            {
                return _viewing;
            }
            set
            {
                this._viewing = value;
                if (value)
                {
                    UnreadCount = 0;
                    HasMentions = false;
                    HasUnread = false;
                }
                NotifyPropertyChanged();
            }
        }

        private bool _viewing = false;
        private bool _hasUnread;
        private bool _hasMentions;
        private int _unreadCount;

        public ChannelStore Store { get; private set; }
        public IBuffer Buffers { get; private set; }
        public Irc Irc { get; }

        public bool ServerLog => Name == "Server";

        public Channel(Irc irc, string channel)
        {
            Name = channel;
            Store = new ChannelStore(this);
            Buffers = irc.CreateChannelBuffer(Name);

            this.Irc = irc;
        }

        public void ClientMessage(string text)
        {
            var msg = new Message();
            msg.User = "";
            msg.Type = MessageType.Info;
            msg.Text = text;
            msg.Channel = Name;

            Buffers.Add(msg);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
