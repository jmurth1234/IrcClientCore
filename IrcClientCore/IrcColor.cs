namespace IrcClientCore
{
    /// <summary>
    /// Represents an IRC color, either from the standard 0-98 palette or a hex RGB value.
    /// </summary>
    public struct IrcColor
    {
        /// <summary>
        /// The palette index (0-98) for standard IRC colors, or null for hex colors.
        /// </summary>
        public int? PaletteIndex { get; private set; }

        /// <summary>
        /// The hex RGB value ("RRGGBB") for hex colors, or null for palette colors.
        /// </summary>
        public string Hex { get; private set; }

        public static IrcColor FromPalette(int index)
        {
            return new IrcColor { PaletteIndex = index };
        }

        public static IrcColor FromHex(string hex)
        {
            return new IrcColor { Hex = hex };
        }

        /// <summary>
        /// Returns the hex RGB string for this color, resolving palette indices via the lookup table.
        /// </summary>
        public string ToHex()
        {
            if (Hex != null) return Hex;
            if (PaletteIndex.HasValue && PaletteIndex.Value >= 0 && PaletteIndex.Value < PaletteToHex.Length)
                return PaletteToHex[PaletteIndex.Value];
            return "000000";
        }

        /// <summary>
        /// Maps IRC color palette indices 0-98 to hex RGB values.
        /// Source: https://modern.ircdocs.horse/formatting
        /// </summary>
        public static readonly string[] PaletteToHex = new string[]
        {
            "FFFFFF", // 00 - White
            "000000", // 01 - Black
            "00007F", // 02 - Blue (Navy)
            "009300", // 03 - Green
            "FF0000", // 04 - Red
            "7F0000", // 05 - Brown (Maroon)
            "9C009C", // 06 - Magenta (Purple)
            "FC7F00", // 07 - Orange (Olive)
            "FFFF00", // 08 - Yellow
            "00FC00", // 09 - Light Green (Lime)
            "009393", // 10 - Cyan (Teal)
            "00FFFF", // 11 - Light Cyan (Aqua)
            "0000FC", // 12 - Light Blue (Royal)
            "FF00FF", // 13 - Pink (Light Purple / Fuchsia)
            "7F7F7F", // 14 - Grey
            "D2D2D2", // 15 - Light Grey (Silver)
            "470000", // 16
            "472100", // 17
            "474700", // 18
            "324700", // 19
            "004700", // 20
            "00472C", // 21
            "004747", // 22
            "002747", // 23
            "000047", // 24
            "2E0047", // 25
            "470047", // 26
            "47002A", // 27
            "740000", // 28
            "743A00", // 29
            "747400", // 30
            "517400", // 31
            "007400", // 32
            "007449", // 33
            "007474", // 34
            "004074", // 35
            "000074", // 36
            "4B0074", // 37
            "740074", // 38
            "740045", // 39
            "B50000", // 40
            "B56300", // 41
            "B5B500", // 42
            "7DB500", // 43
            "00B500", // 44
            "00B571", // 45
            "00B5B5", // 46
            "0063B5", // 47
            "0000B5", // 48
            "7500B5", // 49
            "B500B5", // 50
            "B5006B", // 51
            "FF0000", // 52
            "FF8C00", // 53
            "FFFF00", // 54
            "B2FF00", // 55
            "00FF00", // 56
            "00FFA0", // 57
            "00FFFF", // 58
            "008CFF", // 59
            "0000FF", // 60
            "A500FF", // 61
            "FF00FF", // 62
            "FF0098", // 63
            "FF5959", // 64
            "FFB459", // 65
            "FFFF71", // 66
            "CFFF60", // 67
            "6FFF6F", // 68
            "65FFC9", // 69
            "6DFFFF", // 70
            "59B4FF", // 71
            "5959FF", // 72
            "C459FF", // 73
            "FF66FF", // 74
            "FF59BC", // 75
            "FF9C9C", // 76
            "FFD39C", // 77
            "FFFF9C", // 78
            "E2FF9C", // 79
            "9CFF9C", // 80
            "9CFFDB", // 81
            "9CFFFF", // 82
            "9CD3FF", // 83
            "9C9CFF", // 84
            "DC9CFF", // 85
            "FF9CFF", // 86
            "FF94D3", // 87
            "000000", // 88
            "131313", // 89
            "282828", // 90
            "363636", // 91
            "4D4D4D", // 92
            "656565", // 93
            "818181", // 94
            "9F9F9F", // 95
            "BCBCBC", // 96
            "E2E2E2", // 97
            "FFFFFF", // 98
        };
    }
}
