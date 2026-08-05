using MarkdownParser.Antlr.Answer.Parts;
using System.ComponentModel;

namespace FreeAIr
{
    /// <summary>
    /// Options page storing the font sizes used across the chat UI: context buttons, body text,
    /// code blocks, table headers/bodies and each markdown header level. Values are persisted through
    /// <see cref="BaseOptionModel{T}"/> and surfaced to renderers via <see cref="IFontSizeProvider"/>.
    /// </summary>
    [Browsable(false)]
    public class FontSizePage : BaseOptionModel<FontSizePage>, IFontSizeProvider
    {

        /// <summary>
        /// Font size, in points, used for the clickable context buttons shown alongside chat content.
        /// </summary>
        [Category("Context buttons font sizes")]
        [DisplayName("Button size")]
        [DefaultValue(12)]
        public int ContextButtonSize
        {
            get;
            set;
        } = 12;

        /// <summary>
        /// Font size, in points, used for regular chat message text.
        /// </summary>
        [Category("Other font sizes")]
        [DisplayName("Text size")]
        [DefaultValue(12)]
        public int TextSize
        {
            get;
            set;
        } = 12;

        /// <summary>
        /// Font size, in points, used for multi-line fenced code blocks rendered in chat answers.
        /// </summary>
        [Category("Other font sizes")]
        [DisplayName("Code block size")]
        [DefaultValue(12)]
        public int CodeBlockSize
        {
            get;
            set;
        } = 12;

        /// <summary>
        /// Font size, in points, used for inline code spans within chat text.
        /// </summary>
        [Category("Other font sizes")]
        [DisplayName("Code line size")]
        [DefaultValue(12)]
        public int CodeLineSize
        {
            get;
            set;
        } = 12;

        #region header

        /// <summary>
        /// Font size, in points, for markdown header level 0 (the largest heading).
        /// </summary>
        [Category("Header font sizes")]
        [DisplayName("Header 0 size")]
        [DefaultValue(24)]
        public int Header0Size
        {
            get;
            set;
        } = 24;

        /// <summary>
        /// Font size, in points, for markdown header level 1.
        /// </summary>
        [Category("Header font sizes")]
        [DisplayName("Header 1 size")]
        [DefaultValue(22)]
        public int Header1Size
        {
            get;
            set;
        } = 22;

        /// <summary>
        /// Font size, in points, for markdown header level 2.
        /// </summary>
        [Category("Header font sizes")]
        [DisplayName("Header 2 size")]
        [DefaultValue(20)]
        public int Header2Size
        {
            get;
            set;
        } = 20;

        /// <summary>
        /// Font size, in points, for markdown header level 3.
        /// </summary>
        [Category("Header font sizes")]
        [DisplayName("Header 3 size")]
        [DefaultValue(18)]
        public int Header3Size
        {
            get;
            set;
        } = 18;

        /// <summary>
        /// Font size, in points, for markdown header level 4.
        /// </summary>
        [Category("Header font sizes")]
        [DisplayName("Header 4 size")]
        [DefaultValue(16)]
        public int Header4Size
        {
            get;
            set;
        } = 16;

        /// <summary>
        /// Font size, in points, for markdown header level 5 (the smallest heading).
        /// </summary>
        [Category("Header font sizes")]
        [DisplayName("Header 5 size")]
        [DefaultValue(14)]
        public int Header5Size
        {
            get;
            set;
        } = 14;

        /// <summary>
        /// Looks up the configured font size for a given markdown header level (0 being the largest),
        /// used when rendering headers in chat answers.
        /// </summary>
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

        /// <summary>
        /// Font size, in points, used for markdown table header rows.
        /// </summary>
        [Category("Table sizes")]
        [DisplayName("Table header size")]
        [DefaultValue(12)]
        public int TableHeaderSize
        {
            get;
            set;
        } = 12;

        /// <summary>
        /// Font size, in points, used for markdown table body rows.
        /// </summary>
        [Category("Table sizes")]
        [DisplayName("Table body size")]
        [DefaultValue(12)]
        public int TableBodySize
        {
            get;
            set;
        } = 12;

        #endregion
    }
}
