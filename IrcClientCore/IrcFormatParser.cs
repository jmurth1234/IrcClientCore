using System.Collections.Generic;
using System.Text;

namespace IrcClientCore
{
    /// <summary>
    /// Parses IRC formatting control codes into structured FormattedText.
    /// Supports bold, italic, underline, strikethrough, monospace, color (palette and hex), reverse, and reset.
    /// See https://modern.ircdocs.horse/formatting
    /// </summary>
    public static class IrcFormatParser
    {
        private const char Bold = '\x02';
        private const char Italic = '\x1D';
        private const char Underline = '\x1F';
        private const char Strikethrough = '\x1E';
        private const char Monospace = '\x11';
        private const char Color = '\x03';
        private const char HexColor = '\x04';
        private const char Reverse = '\x16';
        private const char Reset = '\x0F';

        /// <summary>
        /// Parses a string containing IRC formatting control codes into a FormattedText object.
        /// </summary>
        public static FormattedText Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new FormattedText(new List<FormattedSpan>());

            var spans = new List<FormattedSpan>();
            var currentText = new StringBuilder();

            // Current formatting state
            bool bold = false, italic = false, underline = false, strikethrough = false, monospace = false;
            IrcColor? foreground = null, background = null;

            int i = 0;
            while (i < raw.Length)
            {
                char c = raw[i];

                switch (c)
                {
                    case Bold:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        bold = !bold;
                        i++;
                        break;

                    case Italic:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        italic = !italic;
                        i++;
                        break;

                    case Underline:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        underline = !underline;
                        i++;
                        break;

                    case Strikethrough:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        strikethrough = !strikethrough;
                        i++;
                        break;

                    case Monospace:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        monospace = !monospace;
                        i++;
                        break;

                    case Reverse:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        var temp = foreground;
                        foreground = background;
                        background = temp;
                        i++;
                        break;

                    case Reset:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        bold = false;
                        italic = false;
                        underline = false;
                        strikethrough = false;
                        monospace = false;
                        foreground = null;
                        background = null;
                        i++;
                        break;

                    case Color:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        i++;
                        ParsePaletteColor(raw, ref i, ref foreground, ref background);
                        break;

                    case HexColor:
                        FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);
                        i++;
                        ParseHexColor(raw, ref i, ref foreground, ref background);
                        break;

                    default:
                        currentText.Append(c);
                        i++;
                        break;
                }
            }

            // Flush any remaining text
            FlushSpan(spans, currentText, bold, italic, underline, strikethrough, monospace, foreground, background);

            return new FormattedText(spans);
        }

        /// <summary>
        /// Strips all IRC formatting codes from a string, returning plain text.
        /// </summary>
        public static string StripFormatting(string raw)
        {
            return Parse(raw).PlainText;
        }

        private static void FlushSpan(List<FormattedSpan> spans, StringBuilder currentText,
            bool bold, bool italic, bool underline, bool strikethrough, bool monospace,
            IrcColor? foreground, IrcColor? background)
        {
            if (currentText.Length == 0) return;

            spans.Add(new FormattedSpan
            {
                Text = currentText.ToString(),
                Bold = bold,
                Italic = italic,
                Underline = underline,
                Strikethrough = strikethrough,
                Monospace = monospace,
                Foreground = foreground,
                Background = background,
            });

            currentText.Clear();
        }

        /// <summary>
        /// Parses palette color codes after \x03. Format: [FG[,BG]] where FG and BG are 1-2 digit numbers.
        /// When two digits are available, two must always be consumed.
        /// If no digits follow, colors are reset.
        /// </summary>
        private static void ParsePaletteColor(string raw, ref int i, ref IrcColor? foreground, ref IrcColor? background)
        {
            int? fg = TryParseColorNumber(raw, ref i);
            if (!fg.HasValue)
            {
                // \x03 with no digits resets colors
                foreground = null;
                background = null;
                return;
            }

            foreground = IrcColor.FromPalette(fg.Value);

            // Check for comma + background
            if (i < raw.Length && raw[i] == ',')
            {
                int savedPos = i;
                i++; // skip comma
                int? bg = TryParseColorNumber(raw, ref i);
                if (bg.HasValue)
                {
                    background = IrcColor.FromPalette(bg.Value);
                }
                else
                {
                    // Comma but no valid BG number — don't consume the comma
                    i = savedPos;
                }
            }
        }

        /// <summary>
        /// Tries to parse a 1-2 digit color number at position i.
        /// Per spec, when two digits are available, two must always be read.
        /// </summary>
        private static int? TryParseColorNumber(string raw, ref int i)
        {
            if (i >= raw.Length || !IsDigit(raw[i]))
                return null;

            int first = raw[i] - '0';
            i++;

            if (i < raw.Length && IsDigit(raw[i]))
            {
                int second = raw[i] - '0';
                i++;
                return first * 10 + second;
            }

            return first;
        }

        /// <summary>
        /// Parses hex color codes after \x04. Format: [RRGGBB[,RRGGBB]].
        /// If no hex digits follow, colors are reset.
        /// </summary>
        private static void ParseHexColor(string raw, ref int i, ref IrcColor? foreground, ref IrcColor? background)
        {
            string fgHex = TryParseHexValue(raw, ref i);
            if (fgHex == null)
            {
                foreground = null;
                background = null;
                return;
            }

            foreground = IrcColor.FromHex(fgHex);

            if (i < raw.Length && raw[i] == ',')
            {
                int savedPos = i;
                i++; // skip comma
                string bgHex = TryParseHexValue(raw, ref i);
                if (bgHex != null)
                {
                    background = IrcColor.FromHex(bgHex);
                }
                else
                {
                    i = savedPos;
                }
            }
        }

        /// <summary>
        /// Tries to read exactly 6 hex characters at position i.
        /// </summary>
        private static string TryParseHexValue(string raw, ref int i)
        {
            if (i + 6 > raw.Length)
                return null;

            for (int j = 0; j < 6; j++)
            {
                if (!IsHexDigit(raw[i + j]))
                    return null;
            }

            string hex = raw.Substring(i, 6).ToUpper();
            i += 6;
            return hex;
        }

        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private static bool IsHexDigit(char c) =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }
}
