using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>A single inline `` `code` `` span part, rendered as monospaced <c>Run</c> text.</summary>
    public sealed class CodeLinePart : IPart
    {
        /// <summary>The fixed foreground color used to paint code text in the rendered <see cref="Run"/>.</summary>
        private static readonly Brush _foregroundBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));
        /// <summary>Supplies the font size used for the rendered code <see cref="Run"/>, kept in sync with the chat's current text size setting.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;

        /// <summary>Identifies this part as an inline code span for filtering by <see cref="PartTypeEnum"/>.</summary>
        public PartTypeEnum Type => PartTypeEnum.CodeLine;

        /// <summary>The raw inline markdown text of the span as parsed, including the surrounding backticks.</summary>
        public string Text
        {
            get;
        }
        /// <summary>The code content with the surrounding backticks trimmed off.</summary>
        public string Code
        {
            get;
        }

        /// <summary>Builds the part from the parsed backtick-quoted text, stripping the backticks into <see cref="Code"/>.</summary>
        public CodeLinePart(
            IFontSizeProvider fontSizeProvider,
            string code
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            _fontSizeProvider = fontSizeProvider;
            Text = code;
            Code = code.Trim('`');
        }

        /// <summary>The parameter passed to an <see cref="AdditionalCommand"/> button for this part — the backtick-stripped code.</summary>
        public object GetContextForAdditionalCommand()
        {
            return Code;
        }

        /// <summary>Renders the line as a single monospaced <see cref="Run"/>.</summary>
        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            yield return new Run
            {
                FontFamily = new FontFamily("Cascadia Code"),
                Foreground = _foregroundBrush,
                FontSize = _fontSizeProvider.CodeLineSize,
                Text = Text
            };
        }
    }
}
