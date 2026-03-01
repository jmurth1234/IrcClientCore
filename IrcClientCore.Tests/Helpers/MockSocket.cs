using System.Collections.Generic;
using System.Threading.Tasks;

namespace IrcClientCore.Tests.Helpers
{
    public class MockSocket : ISocket
    {
        public List<string> SentLines { get; } = new List<string>();

        public Task Connect() => Task.CompletedTask;

        public Task Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false)
            => Task.CompletedTask;

        public Task WriteLine(string line)
        {
            SentLines.Add(line);
            return Task.CompletedTask;
        }
    }
}
