using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public class IrcServer
    {
        public bool Invalid { get; set; }

        public string Name { get; set; } = "";
        public string Hostname { get; set; } = "";
        public int Port { get; set; } = 6667;
        public bool Ssl { get; set; } = false;
        public bool ShouldReconnect { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string NickservPassword { get; set; } = "";

        // channels are a string seperated by commas
        public string Channels { get; set; } = "";

        public override String ToString()
        {
            return Name;
        }
    }
}
