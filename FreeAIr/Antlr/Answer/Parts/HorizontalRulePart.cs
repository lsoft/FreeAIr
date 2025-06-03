using System.Collections.Generic;
using System.Windows.Documents;

namespace FreeAIr.Antlr.Answer.Parts
{
    public sealed class HorizontalRulePart : IPart
    {
        public PartTypeEnum Type => PartTypeEnum.HorizontalRule;

        public string Text
        {
            get;
        }

        public HorizontalRulePart(
            string text
            )
        {
            Text = text;
        }

        public object GetContextForAdditionalCommand()
        {
            return Text;
        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            throw new InvalidOperationException("This control lives at paragraph-level.");
        }
    }
}
