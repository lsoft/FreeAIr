namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>The kind of inline part within a block; a flags enum so <see cref="AdditionalCommand.PartType"/> can match several kinds at once.</summary>
    [Flags]
    public enum PartTypeEnum
    {
        /// <summary>Plain inline text with no special markup.</summary>
        Text = 1,
        /// <summary>An inline XML/HTML-like tag, e.g. from a `&lt;see&gt;`-style element in the answer.</summary>
        Xml = 2,
        /// <summary>An inline hyperlink.</summary>
        Url = 4,
        /// <summary>A markdown heading line.</summary>
        Header = 8,
        /// <summary>A fenced multi-line code block.</summary>
        CodeBlock = 16,
        /// <summary>A single inline code span (backtick-delimited).</summary>
        CodeLine = 32,
        /// <summary>An inline image reference.</summary>
        Image = 64
    }
}
