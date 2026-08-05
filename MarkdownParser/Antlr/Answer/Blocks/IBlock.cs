using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    /// <summary>An <see cref="IBlock"/> that can accept run-on plain text, e.g. so <see cref="ParsedMarkdown.AddText"/> can append to whichever block is currently open.</summary>
    public interface ITextualBlock : IBlock
    {
        /// <summary>Appends text to this block.</summary>
        void AddText(string text);
    }

    /// <summary>One top-level element of a parsed markdown document (paragraph, blockquote, table, horizontal rule), convertible into a WPF <see cref="Block"/>.</summary>
    public interface IBlock
    {
        /// <summary>Which kind of block this is.</summary>
        BlockTypeEnum Type
        {
            get;
        }

        /// <summary>Builds the WPF block for rendering, attaching any matching command controls from <paramref name="acc"/>.</summary>
        Block? CreateBlock(
            AdditionalCommandContainer? acc,
            bool isInProgress
            );
    }
}