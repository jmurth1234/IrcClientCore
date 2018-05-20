﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public class ChannelStore
    {
        private bool currentlySorting;
        private string _Topic = "";

        public ObservableCollection<User> Users { get; private set; }
        public ObservableCollection<string> RawUsers { get; private set; }
        public ObservableCollection<string> SortedUsers { get; private set; }

        public string Topic
        {
            get => _Topic;
            set
            {
                _Topic = value;
                TopicSetEvent?.Invoke(value);
            }
        }
        public Channel Channel { get; private set; }

        public event Action<string> TopicSetEvent;

        public ChannelStore(Channel channel)
        {
            this.Channel = channel;
            this.Users = new ObservableCollection<User>();
            this.RawUsers = new ObservableCollection<string>();
            this.SortedUsers = new ObservableCollection<string>();
        }

        public void ClearUsers()
        {
            Users.Clear();
            SortedUsers.Clear();
        }

        public void SortUsers()
        {
            if (currentlySorting) return;
            currentlySorting = true;
            var watch = Stopwatch.StartNew();

            SortedUsers.Clear();
            var owners = new List<string>();
            var protect = new List<string>();
            var ops = new List<string>();
            var hops = new List<string>();
            var voiced = new List<string>();
            var users = new List<string>();

            foreach (var user in Users)
            {
                try
                {
                    if (user.Prefix.StartsWith("~"))
                    {
                        owners.Add($"~{user.Nick}");
                    }
                    else if (user.Prefix.StartsWith("&"))
                    {
                        protect.Add($"&{user.Nick}");
                    }
                    else if (user.Prefix.StartsWith("@"))
                    {
                        ops.Add($"@{user.Nick}");
                    }
                    else if (user.Prefix.StartsWith("%"))
                    {
                        hops.Add($"@{user.Nick}");
                    }
                    else if (user.Prefix.StartsWith("+"))
                    {
                        voiced.Add($"+{user.Nick}");
                    }
                    else
                    {
                        users.Add(user.Nick);
                    }
                }
                catch (NullReferenceException ex)
                {
                    if (user.Nick.Length != 1)
                    {
                        Debug.WriteLine($"User's nickname {user.Nick} with prefix type {user.Prefix.Substring(0, 1)} is unhandled");
                    }
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Debug.WriteLine("Elapsed time to sort: " + elapsedMs + "ms");
            watch.Start();

            owners.Sort();
            protect.Sort();
            ops.Sort();
            hops.Sort();
            voiced.Sort();
            users.Sort();

            owners.ForEach(SortedUsers.Add);
            protect.ForEach(SortedUsers.Add);
            ops.ForEach(SortedUsers.Add);
            hops.ForEach(SortedUsers.Add);
            voiced.ForEach(SortedUsers.Add);
            users.ForEach(SortedUsers.Add);

            watch.Stop();
            var elapsedMsOrder = watch.ElapsedMilliseconds;

            Debug.WriteLine("Elapsed time to order: " + elapsedMsOrder + "ms");

            Debug.WriteLine("Total time: " + (elapsedMsOrder + elapsedMs) + "ms");
            currentlySorting = false;

        }

        public void AddUsers(List<string> users)
        {
            users.ForEach(AddUser);
        }

        private void AddUser(string username)
        {
            AddUser(username, false);
        }

        public void AddUser(string username, bool sort)
        {
            if (username.Length == 0)
            {
                return;
            }

            if (!HasUser(username))
            {
                var user = new User
                {
                    FullUsername = username,
                };

                RawUsers.Add(user.Nick);

                Users.Add(user);
                if (sort)
                    SortUsers();
            }
        }

        public bool HasUser(string nick)
        {
            nick = nick.Replace("~","").Replace("&","").Replace("@", "").Replace("%","").Replace("+", "");
            if (nick == "") return false;

            return Users.Any(user => user.Nick == nick);
        }

        public void ChangeUser(string oldNick, string newNick) {
            if (!HasUser(oldNick)) return; 

            var user = Users.First(u => u.Nick == oldNick);

            RemoveUser(user.Nick);

            AddUser(user.Prefix + newNick, true);
        }

        public void ChangePrefix(string nick, string newPrefix)
        {
            if (!HasUser(nick)) return;

            var user = Users.First(u => u.Nick == nick);

            RemoveUser(nick);
            AddUser(newPrefix + user.Nick, true);
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

                SortUsers();
            }
        }

        public void SetTopic(string topic)
        {
            this.Topic = topic;
        }
    }

}
