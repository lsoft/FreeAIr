using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>A fenced ` ```code``` ` block part, rendered as monospaced <c>Run</c> text.</summary>
    public sealed class CodeBlockPart : IPart
    {
        /// <summary>The fixed foreground color used to paint code text in the rendered <see cref="Run"/>.</summary>
        private static readonly Brush _foregroundBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));
        /// <summary>Supplies the font size used for the rendered code <see cref="Run"/>, kept in sync with the chat's current text size setting.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;

        /// <summary>Identifies this part as a fenced code block for filtering by <see cref="PartTypeEnum"/>.</summary>
        public PartTypeEnum Type => PartTypeEnum.CodeBlock;

        /// <summary>The raw inline markdown text of the block as parsed, including the surrounding fences.</summary>
        public string Text
        {
            get;
        }
        /// <summary>The code content between the fences, with the fence markers stripped.</summary>
        public string Code
        {
            get;
        }

        /// <summary>Builds the part from the parsed fenced-block text and its extracted code content.</summary>
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
