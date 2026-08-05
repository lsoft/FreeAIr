using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    /// <summary>A `---` horizontal-rule block; wraps a pre-built WPF element (see <see cref="AnswerMarkdownListener.EnterHorizontal_rule"/>) and returns it as-is.</summary>
    public sealed class HorizontalRuleBlock : IBlock
    {
        /// <summary>The pre-built WPF element representing the rule, returned unchanged by <see cref="CreateBlock"/>.</summary>
        private readonly BlockUIContainer _blockUIContainer;

        /// <summary>Identifies this block as a horizontal rule for block-type dispatch.</summary>
        public BlockTypeEnum Type => BlockTypeEnum.HorizontalRule;

        /// <summary>Wraps an already-built horizontal-rule element.</summary>
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
