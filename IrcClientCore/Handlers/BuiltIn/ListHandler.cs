using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    public class ListHandler : BaseHandler
    {
        private List<ChannelListItem> _channels;
        public override Task<bool> HandleLine(IrcMessage parsedLine)
        {
            switch (parsedLine.CommandMessage.Command)
            {
                case "321":
                    _channels = new List<ChannelListItem>();
                    break;
                case "322":
                    if (_channels == null)
                    {
                        _channels = new List<ChannelListItem>();
                    }

                    var count = 0;
                    int.TryParse(parsedLine.CommandMessage.Parameters[2], out count);

                    _channels.Add(new ChannelListItem
                    {
                        Channel = parsedLine.CommandMessage.Parameters[1],
                        Topic = parsedLine.TrailMessage.TrailingContent,
                        Users = count
                    });
                    break;
                case "323":
                    Irc.HandleDisplayChannelList?.Invoke(_channels);
                    _channels = new List<ChannelListItem>();
                    break;
            }

            return Task.FromResult(true);
        }
    }

    public class ChannelListItem
    {
        public string Channel { get; set; }
        public string Topic { get; set; }
        public int Users { get; set; }
    }
}
