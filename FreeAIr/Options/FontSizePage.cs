using MarkdownParser.Antlr.Answer.Parts;
using System.ComponentModel;
using System.ComponentModel.Composition;

namespace FreeAIr
{
    [Browsable(false)]
    public class FontSizePage : BaseOptionModel<FontSizePage>, IFontSizeProvider
    {

        [Category("Context buttons font sizes")]
        [DisplayName("Button size")]
        [DefaultValue(12)]
        public int ContextButtonSize
        {
            get;
            set;
        } = 12;

        [Category("Other font sizes")]
        [DisplayName("Text size")]
        [DefaultValue(12)]
        public int TextSize
        {
            get;
            set;
        } = 12;

        [Category("Other font sizes")]
        [DisplayName("Code block size")]
        [DefaultValue(12)]
        public int CodeBlockSize
        {
            get;
            set;
        } = 12;

        [Category("Other font sizes")]
        [DisplayName("Code line size")]
        [DefaultValue(12)]
        public int CodeLineSize
        {
            get;
            set;
        } = 12;

        #region header

        [Category("Header font sizes")]
        [DisplayName("Header 0 size")]
        [DefaultValue(24)]
        public int Header0Size
        {
            get;
            set;
        } = 24;

        [Category("Header font sizes")]
        [DisplayName("Header 1 size")]
        [DefaultValue(22)]
        public int Header1Size
        {
            get;
            set;
        } = 22;

        [Category("Header font sizes")]
        [DisplayName("Header 2 size")]
        [DefaultValue(20)]
        public int Header2Size
        {
            get;
            set;
        } = 20;

        [Category("Header font sizes")]
        [DisplayName("Header 3 size")]
        [DefaultValue(18)]
        public int Header3Size
        {
            get;
            set;
        } = 18;

        [Category("Header font sizes")]
        [DisplayName("Header 4 size")]
        [DefaultValue(16)]
        public int Header4Size
        {
            get;
            set;
        } = 16;

        [Category("Header font sizes")]
        [DisplayName("Header 5 size")]
        [DefaultValue(14)]
        public int Header5Size
        {
            get;
            set;
        } = 14;

        public int GetHeaderFontSize(int level)
        {
            return new int[]
            {
                Header0Size,
                Header1Size,
                Header2Size,
                Header3Size,
                Header4Size,
                Header5Size,
            }[level];
        }

        [Category("Table sizes")]
        [DisplayName("Table header size")]
        [DefaultValue(14)]
        public int TableHeaderSize
        {
            get;
            set;
        } = 14;

        [Category("Table sizes")]
        [DisplayName("Table body size")]
        [DefaultValue(14)]
        public int TableBodySize
        {
            get;
            set;
        } = 14;

        #endregion
    }

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
