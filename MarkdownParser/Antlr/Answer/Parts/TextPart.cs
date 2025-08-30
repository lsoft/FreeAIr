using MarkdownParser.Helper;
using System.Windows;
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
            const string boldAnchor = "**";

            var boldIndexes = Text.AllIndexesOf(
                boldAnchor
                )
                .ConvertAll(i => (Bold: true, Index: i));
            if (boldIndexes.Count <= 1)
            {
                yield return new Run
                {
                    FontSize = _fontSizeProvider.TextSize,
                    Text = Text
                };
                yield break;
            }

            if (boldIndexes[0].Index > 0)
            {
                boldIndexes.Insert(
                    0,
                    (Bold: false, Index: 0)
                    );
            }
            if (boldIndexes.Last().Index < Text.Length - boldAnchor.Length - 1)
            {
                boldIndexes.Add(
                    (Bold: false, Index: Text.Length)
                    );
            }

            var currentFontWeight = FontWeights.Regular;
            for (var partIndex = 0; partIndex < boldIndexes.Count - 1; partIndex++)
            {
                var leftPartIndex = partIndex;
                var rightPartIndex = partIndex + 1;

                var leftPair = boldIndexes[leftPartIndex];
                var leftIndex = leftPair.Bold
                    ? leftPair.Index + boldAnchor.Length
                    : leftPair.Index
                    ;

                var rightPair = boldIndexes[rightPartIndex];
                var rightIndex = rightPair.Index;

                if (leftPair.Bold && rightPair.Bold)
                {
                    currentFontWeight =
                        currentFontWeight == FontWeights.Bold
                        ? FontWeights.Regular
                        : FontWeights.Bold
                        ;
                }
                else
                {
                    currentFontWeight = FontWeights.Regular;
                }

                yield return new Run
                {
                    FontSize = _fontSizeProvider.TextSize,
                    FontWeight = currentFontWeight,
                    Text = Text.Substring(leftIndex, rightIndex - leftIndex)
                };
            }
        }
    }
}
