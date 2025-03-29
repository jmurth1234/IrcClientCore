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
            // Get the word being completed
            var words = text.Split(' ');
            var currentWord = words.Last();
            
            // If this word isn't at the start, we shouldn't add a colon
            bool isFirstWord = words.Length == 1 || (words.Length > 1 && string.IsNullOrWhiteSpace(words[0]));
            
            // Get users in the channel
            var users = _handler.Server.GetRawUsers(CurrentChannel);
            
            // Filter users based on the current word being typed
            var matches = users.Where(user => user.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase)).ToArray();
            
            // Format matches with trailing colon if this is the first word
            if (isFirstWord)
            {
                return matches.Select(user => user + ": ").ToArray();
            }
            
            return matches;
        }
    }
}