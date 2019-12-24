using FluentTerminal.Models.Enums;

namespace FluentTerminal.Models
{
    public class TerminalOptions
    {
        public string FontFamily { get; set; }

        public int FontSize { get; set; }

        public int FontWeight { get; set; }

        public CursorStyle CursorStyle { get; set; }

        public bool CursorBlink { get; set; }

        public BellStyle BellStyle { get; set; }

        public ScrollBarStyle ScrollBarStyle { get; set; }

        public double BackgroundOpacity { get; set; }

        public int Padding { get; set; }

        public uint ScrollBackLimit { get; set; }

        public bool ShowTextCopied { get; set; }

        public bool WindowsMode { get; set; } = true;

        public string WordSeparator { get; set; }

        public TerminalOptions Clone() => new TerminalOptions
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight,
            CursorStyle = CursorStyle,
            CursorBlink = CursorBlink,
            BellStyle = BellStyle,
            ScrollBarStyle = ScrollBarStyle,
            BackgroundOpacity = BackgroundOpacity,
            Padding = Padding,
            ScrollBackLimit = ScrollBackLimit,
            ShowTextCopied = ShowTextCopied,
            WindowsMode = WindowsMode,
            WordSeparator = WordSeparator
        };
    }
}