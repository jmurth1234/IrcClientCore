using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IrcClientCore
{
    /// <summary>
    /// Basic class to represent the connection settings for an IRC Server
    /// </summary>
    public class IrcServer
    {
        /// <summary>
        /// Display name of the IRC server
        /// </summary>
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = "";

        [Url]
        public string Hostname { get; set; } = "";

        [Range(1, 65535)]
        public int Port { get; set; } = 6667;

        public bool Ssl { get; set; } = false;
        public bool IgnoreCertErrors { get; set; } = false;
        public bool ShouldReconnect { get; set; } = true;

        [StringLength(32, MinimumLength = 3)]
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string NickservPassword { get; set; } = "";

        // channels are a string seperated by commas
        public string Channels { get; set; } = "";

        public override string ToString()
        {
            return Name;
        }
    }
}
