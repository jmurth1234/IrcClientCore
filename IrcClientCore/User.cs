using System;
using System.Collections.Generic;

namespace IrcClientCore
{
    public class User : IComparable
    {
        internal static Dictionary<string, int> PrefixMap = new Dictionary<string, int>()
        {
            {"~", 5},
            {"&", 4},
            {"@", 3},
            {"%", 2},
            {"+", 1},
            {"", 0}
        };

        public string Prefix
        {
            get
            {
                var prefix = "";

                if (FullUsername != null && FullUsername.Length > Nick.Length)
                {
                    prefix = $"{FullUsername[0]}";
                }

                return prefix;
            }
        }

        public string FullUsername { get; set; }

        public string Nick
        {
            get
            {
                if (string.IsNullOrEmpty(FullUsername))
                    return "";

                var potential = FullUsername.Substring(0, 1);
                if (PrefixMap.ContainsKey(potential))
                {
                    return FullUsername.Replace("~", "").Replace("&", "").Replace("@", "").Replace("%", "").Replace("+", "");
                }

                return FullUsername;
            }
        }

        /// <summary>
        /// Account name if identified (from account-notify/extended-join)
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Real name from extended-join
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// Whether the user is currently away (from away-notify)
        /// </summary>
        public bool IsAway { get; set; }

        /// <summary>
        /// Away message if IsAway is true
        /// </summary>
        public string AwayMessage { get; set; }

        public override string ToString()
        {
            return FullUsername;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            User otherUser = obj as User;
            if (otherUser != null)
            {
                if (this.Prefix.Equals(otherUser.Prefix))
                {
                    return string.Compare(this.Nick, otherUser.Nick);
                }
                else
                {
                    return PrefixMap[otherUser.Prefix].CompareTo(PrefixMap[this.Prefix]);
                }
            }
            else throw new ArgumentException("Object is not a User");
        }
    }
}