using System.Collections.Generic;
using System.Windows.Documents;

namespace FreeAIr.Antlr.Answer
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