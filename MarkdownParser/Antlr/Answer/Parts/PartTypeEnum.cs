namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>The kind of inline part within a block; a flags enum so <see cref="AdditionalCommand.PartType"/> can match several kinds at once.</summary>
    [Flags]
    public enum PartTypeEnum
    {
        Text = 1,
        Xml = 2,
        Url = 4,
        Header = 8,
        CodeBlock = 16,
        CodeLine = 32,
        Image = 64
    }
}
