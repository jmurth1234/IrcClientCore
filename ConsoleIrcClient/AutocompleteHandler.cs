using IrcClientCore.Commands;
using System;
using System.Linq;

namespace ConsoleIrcClient
{
    internal class AutocompleteHandler : IAutoCompleteHandler
    {
        private readonly CommandManager _handler;
        public string CurrentChannel { get; set; }

        public AutocompleteHandler(CommandManager handler)
        {
            this._handler = handler;
        }

        public char[] Separators { get; set; } = new char[] { ' ', '.', '/' };

        public string[] GetSuggestions(string text, int index)
        {
            var array = text.Split(" ");

            if (text.StartsWith("/"))
            {
                var current = array[0];

                if (array.Length > 1)
                {
                    var completions = _handler.GetCompletions(CurrentChannel, array[0], array.Last());
                    return completions.Length > 0 ? completions : GetUserCompletions(text);
                }

                var commands = _handler.CommandList.Where(cmd => cmd.StartsWith(current));
                return commands.Select(command => command.Replace("/", "")).ToArray();
            }

            if ((text.StartsWith("/") && index > 0 || !text.StartsWith("/")) && CurrentChannel != null)
            {
                return GetUserCompletions(text);
            }

            return new string[0];
        }

        private string[] GetUserCompletions(string text)
        {
            var users = _handler.Server.GetRawUsers(CurrentChannel);
            var current = text.Split(" ").Last();
            return users.Where(cmd => cmd.StartsWith(current)).ToArray();
        }

    }
}