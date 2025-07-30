using MarkdownParser.Antlr.Answer.Parts;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    public interface ITextualBlock : IBlock
    {
        void AddText(string text);
    }

    public interface IBlock
    {
        BlockTypeEnum Type
        {
            get;
        }

        Block? CreateBlock(
            AdditionalCommandContainer? acc,
            bool isInProgress
            );
    }
}