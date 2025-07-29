using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Parts
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
