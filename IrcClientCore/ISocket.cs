using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public interface ISocket
    {
        Task Connect();
        Task Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false);
        Task WriteLine(string line);
    }
}
