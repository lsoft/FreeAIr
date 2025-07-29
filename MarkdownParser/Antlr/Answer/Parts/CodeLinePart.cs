using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Parts
{
    public sealed class CodeLinePart : IPart
    {
        private static readonly Brush _foregroundBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));
        private readonly IFontSizeProvider _fontSizeProvider;

        public PartTypeEnum Type => PartTypeEnum.CodeLine;

        public string Text
        {
            get;
        }
        public string Code
        {
            get;
        }

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

        public object GetContextForAdditionalCommand()
        {
            return Code;
        }

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
