using MarkdownParser.Antlr.Answer.Parts;
using System.ComponentModel.Composition;

namespace FreeAIr
{
    [Export(typeof(IFontSizeProvider))]
    public sealed class FontSizeProxy : IFontSizeProvider
    {
        public int ContextButtonSize => FontSizePage.Instance.ContextButtonSize;

        public int CodeBlockSize => FontSizePage.Instance.CodeBlockSize;

        public int CodeLineSize => FontSizePage.Instance.CodeLineSize;

        public int TextSize => FontSizePage.Instance.TextSize;

        public int TableHeaderSize => FontSizePage.Instance.TableHeaderSize;

        public int TableBodySize => FontSizePage.Instance.TableBodySize;

        public int GetHeaderFontSize(int level)
        {
            return FontSizePage.Instance.GetHeaderFontSize(level);
        }
    }
}
