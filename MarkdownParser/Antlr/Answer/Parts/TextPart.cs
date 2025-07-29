using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Parts
{
    public sealed class TextPart : IPart
    {
        private List<string> _text;
        private readonly IFontSizeProvider _fontSizeProvider;

        public PartTypeEnum Type => PartTypeEnum.Text;

        public string Text => string.Join("", _text);

        public TextPart(
            IFontSizeProvider fontSizeProvider,
            string text
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

            _text = [text];
            _fontSizeProvider = fontSizeProvider;
        }

        public void Append(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            _text.Add(text);
        }

        public object GetContextForAdditionalCommand()
        {
            return Text;
        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            yield return new Run
            {
                FontSize = _fontSizeProvider.TextSize,
                Text = Text
            };
        }
    }
}
