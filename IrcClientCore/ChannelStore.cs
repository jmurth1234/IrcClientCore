using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public class ChannelStore : BaseObject
    {
        private string _Topic = "";

        public SortableObservableCollection<User> Users { get; private set; }
        public ObservableCollection<string> RawUsers { get; private set; }

        public string Topic
        {
            get => _Topic;
            set
            {
                _Topic = value;
                NotifyPropertyChanged();
            }
        }
        public Channel Channel { get; private set; }

        public ChannelStore(Channel channel)
        {
            this.Channel = channel;
            this.Users = new SortableObservableCollection<User>();
            this.RawUsers = new ObservableCollection<string>();
        }

        public void ClearUsers()
        {
            Users.Clear();
            RawUsers.Clear();
        }

        public void ReplaceUsers(List<string> usernames)
        {
            ClearUsers();

            var filtered = usernames.FindAll(u => {
                if (u == "") return false;

                return !HasUser(u);
            });

            var processed = filtered.Select(user => new User
            {
                FullUsername = user,
            });

            var users = processed.ToList();

            Users.AddRange(users);
            users.ForEach(user => RawUsers.Add(user.Nick));
        }

        public void AddUser(string username)
        {
            if (username == null || username.Length == 0)
            {
                return;
            }

            if (!HasUser(username))
            {
                var user = new User
                {
                    FullUsername = username,
                };

                Users.Add(user);
                Users.Sort();

                RawUsers.Add(user.Nick);
            }
        }

        public bool HasUser(string nick)
        {
            nick = nick.Replace("~", "").Replace("&", "").Replace("@", "").Replace("%", "").Replace("+", "");
            if (nick == "")
            {
                return false;
            }

            return Users.Any(user => user.Nick == nick);
        }

        public void ChangeUser(string oldNick, string newNick)
        {
            if (!HasUser(oldNick)) return;

            var user = Users.First(u => u.Nick == oldNick);

            RemoveUser(user.Nick);

            AddUser(user.Prefix + newNick);
        }

        public void ChangePrefix(string nick, string newPrefix)
        {
            if (!HasUser(nick)) return;

            var user = Users.First(u => u.Nick == nick);

            RemoveUser(nick);
            AddUser(newPrefix + user.Nick);
        }

        internal string GetPrefix(string user)
        {
            if (HasUser(user))
                return Users.First(u => u.Nick == user).Prefix;
            else return "";
        }

        public void RemoveUser(string nick)
        {
            if (Users.Any(user => user.Nick == nick))
            {
                var user = Users.First(u => u.Nick == nick);

                Users.Remove(user);
                RawUsers.Remove(user.Nick);
            }
        }

        public void SetTopic(string topic)
        {
            Channel.Irc.CommandManager.HandleCommand(Channel.Name, $"/topic {topic}");
            this.Topic = topic;
        }
    }

    /// <summary>
    /// SortableObservableCollection is a ObservableCollection with some extensions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Adds a range of items to the observable collection.
        /// Instead of iterating through all elements and adding them
        /// one by one (which causes OnPropertyChanged events), all
        /// the items gets added instantly without firing events.
        /// After adding all elements, the OnPropertyChanged event will be fired.
        /// </summary>
        /// <param name="enumerable"></param>
        public void AddRange(IEnumerable<T> enumerable)
        {
            CheckReentrancy();

            int startIndex = Count;

            foreach (var item in enumerable)
                Items.Add(item);

            (Items as List<T>).Sort();

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(enumerable), startIndex));
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }

        public void Sort()
        {
            CheckReentrancy();

            var old = new List<T>(Items);
            (Items as List<T>).Sort();

            // publish the changed order of the list
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, old, Items, 0));
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }
    }
}
