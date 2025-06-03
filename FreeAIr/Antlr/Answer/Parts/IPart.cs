using System.Collections.Generic;
using System.Windows.Documents;

namespace FreeAIr.Antlr.Answer.Parts
{
    public interface IPart
    {
        PartTypeEnum Type
        {
            get;
        }

        string Text
        {
            get;
        }

        object GetContextForAdditionalCommand();

        IEnumerable<Inline> GetInlines(
            bool isInProgress
            );
    }
}
