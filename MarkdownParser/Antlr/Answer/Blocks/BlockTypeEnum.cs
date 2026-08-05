namespace MarkdownParser.Antlr.Answer.Blocks
{
    /// <summary>The kind of top-level <see cref="IBlock"/> a parsed markdown document is broken into.</summary>
    public enum BlockTypeEnum
    {
        Paragraph,
        Blockquote,
        HorizontalRule,
        Table
    }
}
