using System.Collections.Generic;
using System.Threading.Tasks;

namespace IrcClientCore.Tests.Helpers
{
    /// <summary>
    /// Testable subclass of Irc that injects mock socket and buffer,
    /// capturing all outgoing writes and messages.
    /// </summary>
    public class TestableIrc : Irc
    {
        public MockSocket MockSocket { get; } = new MockSocket();
        public Dictionary<string, MockBuffer> MockBuffers { get; } = new Dictionary<string, MockBuffer>(System.StringComparer.OrdinalIgnoreCase);

        public TestableIrc(IrcServer server) : base(server) { }

        public override ISocket CreateConnection() => MockSocket;

        public override IBuffer CreateChannelBuffer(string channel)
        {
            var buffer = new MockBuffer();
            MockBuffers[channel] = buffer;
            return buffer;
        }

        /// <summary>
        /// Initialises the Irc instance and waits for the async-void setup to complete.
        /// </summary>
        public async Task InitialiseAsync()
        {
            Initialise();
            await Task.Delay(50);
        }

        /// <summary>
        /// Simulates receiving a raw line from the server.
        /// </summary>
        public Task SimulateReceive(string rawLine) => RecieveLine(rawLine);

        /// <summary>
        /// Returns sent lines collected after a given index (useful when you want to
        /// ignore setup lines like CAP LS 302 and check only subsequent sends).
        /// </summary>
        public List<string> SentLinesAfter(int startIndex)
            => MockSocket.SentLines.GetRange(startIndex, MockSocket.SentLines.Count - startIndex);
    }
}
