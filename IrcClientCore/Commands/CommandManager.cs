using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrcClientCore.Commands
{
    public class CommandManager
    {
        private Dictionary<String, BaseCommand> _commandTable = new Dictionary<String, BaseCommand>();
        public ICollection<string> CommandList => _commandTable.Keys;

        public Irc Server { get; private set; }

        public CommandManager(Irc irc)
        {
            this.Server = irc;
            RegisterCommand("/help", new HelpCommand());

            RegisterCommand("/me", new MeCommand());
            RegisterCommand("/join", new JoinCommand());
            RegisterCommand("/part", new PartCommand());
            RegisterCommand("/quit", new QuitCommand());
            RegisterCommand("/query", new QueryCommand());
            RegisterCommand("/nick", new NickCommand());
            RegisterCommand("/msg", new MsgCommand());
            RegisterCommand("/whois", new WhoisCommand());

            RegisterCommand("/mode", new ModeCommand());

            RegisterCommand("/op", new OpCommand());
            RegisterCommand("/deop", new OpCommand());
            RegisterCommand("/voice", new VoiceCommand());
            RegisterCommand("/devoice", new VoiceCommand());
            RegisterCommand("/mute", new QuietCommand());
            RegisterCommand("/unmute", new QuietCommand());

            RegisterCommand("/kick", new KickCommand());
            RegisterCommand("/ban", new BanCommand());

            RegisterCommand("/raw", new RawCommand());
        }

        public void RegisterCommand(string cmd, BaseCommand handler)
        {
            handler.Irc = Server;
            _commandTable.Add(cmd, handler);
        }

        public string[] GetCompletions(string command, string arg)
        {
            var cmd = GetCommand(command);
            if (cmd == null)
            {
                return new string[0];
            }

            var completions = cmd.GetCompletions(arg);

            return completions ?? new string[0];
        }

        internal BaseCommand GetCommand(string potentialCommand)
        {
            var cmd = CommandList.Where(command => command.StartsWith(potentialCommand)).ToList();
            var channel = Server.ChannelList[Server.CurrentChannel];
            if (cmd.Count > 1)
            {
                channel.ClientMessage("Multiple matches found: " + potentialCommand);
                channel.ClientMessage(String.Join(", ", cmd));
                channel.ClientMessage("Type /help for a list of commands.");
            }
            else if (cmd.Count == 1)
            {
                return _commandTable[cmd[0]];
            }
            else
            {
                channel.ClientMessage("Unknown Command: " + potentialCommand);
                channel.ClientMessage("Type /help for a list of commands.");
            }
            return null;
        }

        public void HandleCommand(string text)
        {
            string[] args = text.Split(' ');
            
            if (args[0].StartsWith("//") || !args[0].StartsWith("/"))
            {
                if (args[0].StartsWith("//"))
                    args[0] = args[0].Replace("//", "/");
                Server.SendMessage(String.Join(" ", args));
            }
            else if (args[0].StartsWith("/"))
            {
                var command = GetCommand(args[0]);
                command.RunCommand(args);
            }
        }

    }
}
