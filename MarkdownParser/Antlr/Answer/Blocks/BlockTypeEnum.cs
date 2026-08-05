namespace MarkdownParser.Antlr.Answer.Blocks
{
    /// <summary>The kind of top-level <see cref="IBlock"/> a parsed markdown document is broken into.</summary>
    public enum BlockTypeEnum
    {
        /// <summary>A regular paragraph of flowing text and inline parts.</summary>
        Paragraph,
        /// <summary>A `&gt;` blockquote block.</summary>
        Blockquote,
        /// <summary>A `---` horizontal rule.</summary>
        HorizontalRule,
        /// <summary>A markdown pipe-delimited table.</summary>
        Table
    }
}
