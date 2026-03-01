using IrcClientCore;
using System;
using System.Text;

namespace ConsoleIrcClient
{
    /// <summary>
    /// Renders FormattedText to the console using ANSI escape codes.
    /// Supports bold, italic, underline, strikethrough, and 256-color palette.
    /// </summary>
    internal static class AnsiFormatter
    {
        private const string Esc = "\x1b[";
        private const string ResetAll = "\x1b[0m";

        /// <summary>
        /// Writes a FormattedText to console with ANSI formatting.
        /// Falls back to plain text if FormattedText is null.
        /// </summary>
        public static void WriteFormatted(Message message)
        {
            if (message.FormattedText == null || message.FormattedText.Spans.Count == 0)
            {
                Console.Write(message.Text);
                return;
            }

            foreach (var span in message.FormattedText.Spans)
            {
                var codes = new StringBuilder();

                if (span.Bold) codes.Append($"{Esc}1m");
                if (span.Italic) codes.Append($"{Esc}3m");
                if (span.Underline) codes.Append($"{Esc}4m");
                if (span.Strikethrough) codes.Append($"{Esc}9m");

                if (span.Foreground.HasValue)
                {
                    var hex = span.Foreground.Value.ToHex();
                    var (r, g, b) = ParseHex(hex);
                    codes.Append($"{Esc}38;2;{r};{g};{b}m");
                }

                if (span.Background.HasValue)
                {
                    var hex = span.Background.Value.ToHex();
                    var (r, g, b) = ParseHex(hex);
                    codes.Append($"{Esc}48;2;{r};{g};{b}m");
                }

                if (codes.Length > 0)
                {
                    Console.Write(codes.ToString());
                    Console.Write(span.Text);
                    Console.Write(ResetAll);
                }
                else
                {
                    Console.Write(span.Text);
                }
            }
        }

        private static (int r, int g, int b) ParseHex(string hex)
        {
            if (hex == null || hex.Length < 6) return (255, 255, 255);
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return (r, g, b);
        }
    }
}
