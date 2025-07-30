using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    public sealed class HorizontalRuleBlock : IBlock
    {
        private readonly BlockUIContainer _blockUIContainer;

        public BlockTypeEnum Type => BlockTypeEnum.HorizontalRule;

        public HorizontalRuleBlock(
            BlockUIContainer blockUIContainer
            )
        {
            if (blockUIContainer is null)
            {
                throw new ArgumentNullException(nameof(blockUIContainer));
            }

            _blockUIContainer = blockUIContainer;
        }

        public System.Windows.Documents.Block CreateBlock(
            AdditionalCommandContainer? acc,
            bool isInProgress
            )
        {
            return _blockUIContainer;
        }
    }
}
