using System.Linq;
using Xunit;

namespace IrcClientCore.Tests
{
    /// <summary>
    /// Tests for IRC formatting code parser.
    /// See https://modern.ircdocs.horse/formatting
    /// </summary>
    public class IrcFormatParserTests
    {
        // ── Plain Text ──────────────────────────────────────────────────────────

        [Fact]
        public void Parse_PlainText_SingleSpanNoFormatting()
        {
            var result = IrcFormatParser.Parse("Hello World");
            Assert.Single(result.Spans);
            Assert.Equal("Hello World", result.Spans[0].Text);
            Assert.False(result.Spans[0].Bold);
            Assert.Equal("Hello World", result.PlainText);
        }

        [Fact]
        public void Parse_EmptyString_NoSpans()
        {
            var result = IrcFormatParser.Parse("");
            Assert.Empty(result.Spans);
            Assert.Equal("", result.PlainText);
        }

        [Fact]
        public void Parse_Null_NoSpans()
        {
            var result = IrcFormatParser.Parse(null);
            Assert.Empty(result.Spans);
        }

        // ── Bold ────────────────────────────────────────────────────────────────

        [Fact]
        public void Parse_Bold_TogglesOn()
        {
            var result = IrcFormatParser.Parse("\u0002Bold Text");
            Assert.Single(result.Spans);
            Assert.Equal("Bold Text", result.Spans[0].Text);
            Assert.True(result.Spans[0].Bold);
        }

        [Fact]
        public void Parse_Bold_TogglesOnAndOff()
        {
            var result = IrcFormatParser.Parse("\u0002Bold\u0002 Normal");
            Assert.Equal(2, result.Spans.Count);
            Assert.Equal("Bold", result.Spans[0].Text);
            Assert.True(result.Spans[0].Bold);
            Assert.Equal(" Normal", result.Spans[1].Text);
            Assert.False(result.Spans[1].Bold);
        }

        [Fact]
        public void Parse_PlainText_ReturnsCorrectly()
        {
            var result = IrcFormatParser.Parse("\u0002Bold\u0002 Normal");
            Assert.Equal("Bold Normal", result.PlainText);
        }

        // ── Italic ──────────────────────────────────────────────────────────────

        [Fact]
        public void Parse_Italic_TogglesOn()
        {
            var result = IrcFormatParser.Parse("\u001DItalic");
            Assert.Single(result.Spans);
            Assert.True(result.Spans[0].Italic);
            Assert.Equal("Italic", result.Spans[0].Text);
        }

        // ── Underline ───────────────────────────────────────────────────────────

        [Fact]
        public void Parse_Underline_TogglesOn()
        {
            var result = IrcFormatParser.Parse("\u001FUnderlined");
            Assert.Single(result.Spans);
            Assert.True(result.Spans[0].Underline);
        }

        // ── Strikethrough ───────────────────────────────────────────────────────

        [Fact]
        public void Parse_Strikethrough_TogglesOn()
        {
            var result = IrcFormatParser.Parse("\u001EStruck");
            Assert.Single(result.Spans);
            Assert.True(result.Spans[0].Strikethrough);
        }

        // ── Monospace ───────────────────────────────────────────────────────────

        [Fact]
        public void Parse_Monospace_TogglesOn()
        {
            var result = IrcFormatParser.Parse("\u0011Code");
            Assert.Single(result.Spans);
            Assert.True(result.Spans[0].Monospace);
        }

        // ── Nested Formatting ───────────────────────────────────────────────────

        [Fact]
        public void Parse_BoldAndItalic_BothSet()
        {
            var result = IrcFormatParser.Parse("\u0002\u001DBold Italic");
            Assert.Single(result.Spans);
            Assert.True(result.Spans[0].Bold);
            Assert.True(result.Spans[0].Italic);
        }

        [Fact]
        public void Parse_BoldThenItalic_CreatesMultipleSpans()
        {
            var result = IrcFormatParser.Parse("\u0002Bold \u001DBoth\u0002 Italic Only");
            Assert.Equal(3, result.Spans.Count);
            Assert.True(result.Spans[0].Bold);
            Assert.False(result.Spans[0].Italic);
            Assert.True(result.Spans[1].Bold);
            Assert.True(result.Spans[1].Italic);
            Assert.False(result.Spans[2].Bold);
            Assert.True(result.Spans[2].Italic);
        }

        // ── Reset ───────────────────────────────────────────────────────────────

        [Fact]
        public void Parse_Reset_ClearsAllFormatting()
        {
            var result = IrcFormatParser.Parse("\u0002\u001DBold Italic\u000FNormal");
            Assert.Equal(2, result.Spans.Count);
            Assert.True(result.Spans[0].Bold);
            Assert.True(result.Spans[0].Italic);
            Assert.False(result.Spans[1].Bold);
            Assert.False(result.Spans[1].Italic);
            Assert.Equal("Normal", result.Spans[1].Text);
        }

        // ── Colors (palette) ────────────────────────────────────────────────────

        [Fact]
        public void Parse_Color_SingleDigitForeground()
        {
            var result = IrcFormatParser.Parse("\u00034Red");
            Assert.Single(result.Spans);
            Assert.NotNull(result.Spans[0].Foreground);
            Assert.Equal(4, result.Spans[0].Foreground.Value.PaletteIndex);
            Assert.Null(result.Spans[0].Background);
            Assert.Equal("Red", result.Spans[0].Text);
        }

        [Fact]
        public void Parse_Color_TwoDigitForeground()
        {
            // Two digits available, both must be consumed
            var result = IrcFormatParser.Parse("\u000304Red");
            Assert.Single(result.Spans);
            Assert.Equal(4, result.Spans[0].Foreground.Value.PaletteIndex);
            Assert.Equal("Red", result.Spans[0].Text);
        }

        [Fact]
        public void Parse_Color_ForegroundAndBackground()
        {
            var result = IrcFormatParser.Parse("\u00034,12Text");
            Assert.Single(result.Spans);
            Assert.Equal(4, result.Spans[0].Foreground.Value.PaletteIndex);
            Assert.Equal(12, result.Spans[0].Background.Value.PaletteIndex);
        }

        [Fact]
        public void Parse_Color_TwoDigitForegroundAndBackground()
        {
            var result = IrcFormatParser.Parse("\u000304,02Blue on Red");
            Assert.Single(result.Spans);
            Assert.Equal(4, result.Spans[0].Foreground.Value.PaletteIndex);
            Assert.Equal(2, result.Spans[0].Background.Value.PaletteIndex);
        }

        [Fact]
        public void Parse_Color_ResetColors()
        {
            // \x03 with no digits resets colors
            var result = IrcFormatParser.Parse("\u00034Red\u0003Normal");
            Assert.Equal(2, result.Spans.Count);
            Assert.NotNull(result.Spans[0].Foreground);
            Assert.Null(result.Spans[1].Foreground);
            Assert.Null(result.Spans[1].Background);
        }

        [Fact]
        public void Parse_Color_PlainTextCorrect()
        {
            var result = IrcFormatParser.Parse("\u000304Red\u0003 Normal");
            Assert.Equal("Red Normal", result.PlainText);
        }

        [Fact]
        public void Parse_Color_CommaButNoBackground_DoesNotConsumeComma()
        {
            // \x03 4 , followed by non-digit — comma should not be consumed
            var result = IrcFormatParser.Parse("\u00034,Hello");
            Assert.Single(result.Spans);
            Assert.Equal(",Hello", result.Spans[0].Text);
            Assert.Equal(4, result.Spans[0].Foreground.Value.PaletteIndex);
        }

        // ── Hex Colors ──────────────────────────────────────────────────────────

        [Fact]
        public void Parse_HexColor_Foreground()
        {
            var result = IrcFormatParser.Parse("\u0004FF0000Red");
            Assert.Single(result.Spans);
            Assert.Equal("FF0000", result.Spans[0].Foreground.Value.Hex);
            Assert.Equal("Red", result.Spans[0].Text);
        }

        [Fact]
        public void Parse_HexColor_ForegroundAndBackground()
        {
            var result = IrcFormatParser.Parse("\u0004FF0000,00FF00Text");
            Assert.Single(result.Spans);
            Assert.Equal("FF0000", result.Spans[0].Foreground.Value.Hex);
            Assert.Equal("00FF00", result.Spans[0].Background.Value.Hex);
        }

        [Fact]
        public void Parse_HexColor_Reset()
        {
            var result = IrcFormatParser.Parse("\u0004FF0000Red\u0004Normal");
            Assert.Equal(2, result.Spans.Count);
            Assert.NotNull(result.Spans[0].Foreground);
            Assert.Null(result.Spans[1].Foreground);
        }

        // ── Reverse ─────────────────────────────────────────────────────────────

        [Fact]
        public void Parse_Reverse_SwapsForegroundAndBackground()
        {
            var result = IrcFormatParser.Parse("\u00034,12Text\u0016Reversed");
            Assert.Equal(2, result.Spans.Count);
            // First span: fg=4, bg=12
            Assert.Equal(4, result.Spans[0].Foreground.Value.PaletteIndex);
            Assert.Equal(12, result.Spans[0].Background.Value.PaletteIndex);
            // Second span: fg=12, bg=4 (swapped)
            Assert.Equal(12, result.Spans[1].Foreground.Value.PaletteIndex);
            Assert.Equal(4, result.Spans[1].Background.Value.PaletteIndex);
        }

        // ── StripFormatting ─────────────────────────────────────────────────────

        [Fact]
        public void StripFormatting_RemovesAllCodes()
        {
            var raw = "\u0002Bold \u001DItalic \u000304,12Colored\u000F Normal";
            var stripped = IrcFormatParser.StripFormatting(raw);
            Assert.Equal("Bold Italic Colored Normal", stripped);
        }

        // ── Edge Cases ──────────────────────────────────────────────────────────

        [Fact]
        public void Parse_FormatCodeAtEndOfString_NoEmptySpan()
        {
            var result = IrcFormatParser.Parse("Text\u0002");
            Assert.Single(result.Spans);
            Assert.Equal("Text", result.Spans[0].Text);
        }

        [Fact]
        public void Parse_ConsecutiveFormatCodes_NoEmptySpans()
        {
            var result = IrcFormatParser.Parse("\u0002\u001D\u001FFormatted");
            Assert.Single(result.Spans);
            Assert.True(result.Spans[0].Bold);
            Assert.True(result.Spans[0].Italic);
            Assert.True(result.Spans[0].Underline);
        }

        [Fact]
        public void Parse_OnlyFormatCodes_NoSpans()
        {
            var result = IrcFormatParser.Parse("\u0002\u001D\u000F");
            Assert.Empty(result.Spans);
            Assert.Equal("", result.PlainText);
        }

        // ── IrcColor ────────────────────────────────────────────────────────────

        [Fact]
        public void IrcColor_PaletteToHex_ResolvesStandardColors()
        {
            Assert.Equal("FF0000", IrcColor.PaletteToHex[4]); // Red
            Assert.Equal("FFFFFF", IrcColor.PaletteToHex[0]); // White
            Assert.Equal("000000", IrcColor.PaletteToHex[1]); // Black
        }

        [Fact]
        public void IrcColor_ToHex_ResolvesPaletteIndex()
        {
            var color = IrcColor.FromPalette(4);
            Assert.Equal("FF0000", color.ToHex());
        }

        [Fact]
        public void IrcColor_ToHex_ReturnsDirectHex()
        {
            var color = IrcColor.FromHex("AABBCC");
            Assert.Equal("AABBCC", color.ToHex());
        }
    }
}
