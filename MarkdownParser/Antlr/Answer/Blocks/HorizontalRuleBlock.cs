using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    /// <summary>A `---` horizontal-rule block; wraps a pre-built WPF element (see <see cref="AnswerMarkdownListener.EnterHorizontal_rule"/>) and returns it as-is.</summary>
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

        /// <summary>Returns the pre-built element unchanged; there is no per-part content to add commands to.</summary>
        public System.Windows.Documents.Block CreateBlock(
            AdditionalCommandContainer? acc,
            bool isInProgress
            )
        {
            return _blockUIContainer;
        }
    }
}
