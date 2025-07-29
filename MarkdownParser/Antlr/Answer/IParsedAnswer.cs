using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer
{
    public interface IParsedAnswer
    {
        IReadOnlyList<Block> Blocks
        {
            get;
        }

        FlowDocument ConvertToFlowDocument(
            AdditionalCommandContainer? acc,
            bool isInProgress
            );
    }
}