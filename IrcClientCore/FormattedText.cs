using System.Collections.Generic;
using System.Linq;

namespace IrcClientCore
{
    /// <summary>
    /// Represents IRC-formatted text as an ordered list of formatted spans,
    /// along with a plain text representation with all formatting stripped.
    /// </summary>
    public class FormattedText
    {
        public IReadOnlyList<FormattedSpan> Spans { get; }
        public string PlainText { get; }

        public FormattedText(IReadOnlyList<FormattedSpan> spans)
        {
            Spans = spans;
            PlainText = string.Concat(spans.Select(s => s.Text));
        }
    }
}
