using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    internal class ChannelForwardHandler : BaseHandler
    {
        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            Irc.RemoveChannel(parsedLine.CommandMessage.Parameters[1]);
            return await Irc.AddChannel(parsedLine.CommandMessage.Parameters[2]);
        }
    }
}