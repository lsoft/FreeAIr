namespace MarkdownParser.Antlr.Answer.Parts
{
    public interface IFontSizeProvider
    {
        int ContextButtonSize
        {
            get;
        }

        int CodeBlockSize
        {
            get;
        }

        int CodeLineSize
        {
            get;
        }

        int TextSize
        {
            get;
        }

        int TableHeaderSize
        {
            get;
        }

        int TableBodySize
        {
            get;
        }

        int GetHeaderFontSize(int level);
    }

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
