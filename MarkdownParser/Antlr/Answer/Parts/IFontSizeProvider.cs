namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>
    /// Supplies the font sizes markdown blocks and parts use when rendering into a WPF
    /// <see cref="System.Windows.Documents.FlowDocument"/>, so the host UI can control text scale
    /// without the block/part classes hard-coding sizes.
    /// </summary>
    public interface IFontSizeProvider
    {
        /// <summary>Font size for inline action buttons rendered inside the answer (e.g. copy-code buttons).</summary>
        int ContextButtonSize
        {
            get;
        }

        /// <summary>Font size for fenced code blocks.</summary>
        int CodeBlockSize
        {
            get;
        }

        /// <summary>Font size for individual code lines.</summary>
        int CodeLineSize
        {
            get;
        }

        /// <summary>Font size for regular paragraph text.</summary>
        int TextSize
        {
            get;
        }

        /// <summary>Font size for table header cells.</summary>
        int TableHeaderSize
        {
            get;
        }

        /// <summary>Font size for table body cells.</summary>
        int TableBodySize
        {
            get;
        }

        /// <summary>Font size for a header of the given level (1-6).</summary>
        int GetHeaderFontSize(int level);
    }

    /// <summary>Fixed-size <see cref="IFontSizeProvider"/> used wherever no per-instance font scaling is needed.</summary>
    public sealed class ConstantFontSizeProvider : IFontSizeProvider
    {
        public static readonly ConstantFontSizeProvider Instance = new();

        public int ContextButtonSize => 14;

        public int CodeBlockSize => 14;

        public int CodeLineSize => 14;

        public int TextSize => 14;

        public int TableHeaderSize => 18;

        public int TableBodySize => 14;

        public int GetHeaderFontSize(int level)
        {
            return 24 - level;
        }
    }
}
