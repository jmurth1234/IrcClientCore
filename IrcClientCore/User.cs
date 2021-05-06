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

                if (FullUsername.Length > Nick.Length)
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
                var potential = FullUsername.Substring(0, 1);
                if (PrefixMap.ContainsKey(potential))
                {
                    return FullUsername.Replace("~", "").Replace("&", "").Replace("@", "").Replace("%", "").Replace("+", "");
                }

                return FullUsername;
            }
        }

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