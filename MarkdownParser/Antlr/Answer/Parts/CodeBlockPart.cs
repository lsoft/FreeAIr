using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>A fenced ` ```code``` ` block part, rendered as monospaced <c>Run</c> text.</summary>
    public sealed class CodeBlockPart : IPart
    {
        private static readonly Brush _foregroundBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));
        private readonly IFontSizeProvider _fontSizeProvider;

        public PartTypeEnum Type => PartTypeEnum.CodeBlock;

        public string Text
        {
            get;
        }
        public string Code
        {
            get;
        }

        public CodeBlockPart(
            IFontSizeProvider fontSizeProvider,
            string text,
            string code
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            _fontSizeProvider = fontSizeProvider;
            Text = text;
            Code = code;
        }

        /// <summary>The parameter passed to an <see cref="AdditionalCommand"/> button for this part — the raw code.</summary>
        public object GetContextForAdditionalCommand()
        {
            return Code;
        }

        /// <summary>Renders the code as a single monospaced <see cref="Run"/>.</summary>
        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            yield return new Run
            {
                FontFamily = new FontFamily("Cascadia Code"),
                Foreground = _foregroundBrush,
                FontSize = _fontSizeProvider.CodeBlockSize,
                Text = Code
            };
        }
    }
}
