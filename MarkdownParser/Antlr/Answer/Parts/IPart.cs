using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>One inline element within a block (text, header, code, link, image, XML node), convertible into WPF <see cref="Inline"/>s.</summary>
    public interface IPart
    {
        /// <summary>Which kind of part this is, matched against <see cref="AdditionalCommand.PartType"/>.</summary>
        PartTypeEnum Type
        {
            get;
        }

        /// <summary>The part's raw source text.</summary>
        string Text
        {
            get;
        }

        /// <summary>
        /// The value passed as the command parameter to an <see cref="AdditionalCommand"/> button
        /// attached to this part. Null when the part has nothing to offer — the button is still
        /// shown, and its command has to cope with a null parameter.
        /// </summary>
        object? GetContextForAdditionalCommand();

        /// <summary>Builds the WPF inlines that render this part.</summary>
        IEnumerable<Inline> GetInlines(
            bool isInProgress
            );
    }
}
