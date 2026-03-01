namespace IrcClientCore
{
    /// <summary>
    /// Represents a contiguous run of text with uniform IRC formatting.
    /// </summary>
    public class FormattedSpan
    {
        public string Text { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }
        public bool Monospace { get; set; }
        public IrcColor? Foreground { get; set; }
        public IrcColor? Background { get; set; }
    }
}
