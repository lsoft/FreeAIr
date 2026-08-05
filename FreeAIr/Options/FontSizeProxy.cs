using MarkdownParser.Antlr.Answer.Parts;
using System.ComponentModel.Composition;

namespace FreeAIr
{
    /// <summary>
    /// MEF-exported <see cref="IFontSizeProvider"/> that forwards every font size query to the
    /// singleton <see cref="FontSizePage"/> options page, letting renderers depend on the interface
    /// without referencing the VS options infrastructure directly.
    /// </summary>
    [Export(typeof(IFontSizeProvider))]
    public sealed class FontSizeProxy : IFontSizeProvider
    {
        /// <summary>
        /// Font size configured for context buttons, read from <see cref="FontSizePage"/>.
        /// </summary>
        public int ContextButtonSize => FontSizePage.Instance.ContextButtonSize;

        /// <summary>
        /// Font size configured for code blocks, read from <see cref="FontSizePage"/>.
        /// </summary>
        public int CodeBlockSize => FontSizePage.Instance.CodeBlockSize;

        /// <summary>
        /// Font size configured for inline code spans, read from <see cref="FontSizePage"/>.
        /// </summary>
        public int CodeLineSize => FontSizePage.Instance.CodeLineSize;

        /// <summary>
        /// Font size configured for regular text, read from <see cref="FontSizePage"/>.
        /// </summary>
        public int TextSize => FontSizePage.Instance.TextSize;

        /// <summary>
        /// Font size configured for table headers, read from <see cref="FontSizePage"/>.
        /// </summary>
        public int TableHeaderSize => FontSizePage.Instance.TableHeaderSize;

        /// <summary>
        /// Font size configured for table body cells, read from <see cref="FontSizePage"/>.
        /// </summary>
        public int TableBodySize => FontSizePage.Instance.TableBodySize;

        /// <summary>
        /// Delegates to <see cref="FontSizePage.GetHeaderFontSize"/> to resolve the font size for
        /// the given markdown header level.
        /// </summary>
        public int GetHeaderFontSize(int level)
        {
            return FontSizePage.Instance.GetHeaderFontSize(level);
        }
    }
}
