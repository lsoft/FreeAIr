using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    public sealed class BlockquoteBlock : IBlock, ITextualBlock
    {
        private static readonly Brush _semiTransparentGray = new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80));

        private readonly List<IPart> _parts = new();
        private readonly IFontSizeProvider _fontSizeProvider;
        private BlockUIContainer? _blockContainer;

        public BlockTypeEnum Type => BlockTypeEnum.Blockquote;

        public BlockquoteBlock(
            IFontSizeProvider fontSizeProvider
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            _fontSizeProvider = fontSizeProvider;
        }

        public void AddText(string text)
        {
            if (_parts.Count > 0)
            {
                var lastPart = _parts.Last();
                if (lastPart is TextPart lastText)
                {
                    lastText.Append(text);
                    return;
                }
            }

            _parts.Add(new TextPart(_fontSizeProvider, text));
        }


        public System.Windows.Documents.Block CreateBlock(
            AdditionalCommandContainer? acc,
            bool isInProgress
            )
        {
            if (_blockContainer is not null)
            {
                return _blockContainer;
            }

            var paragraph = new Paragraph
            {
                Margin = new Thickness(10, 0, 0, 0),
                Background = _semiTransparentGray,
                BorderBrush = Brushes.Green,
                BorderThickness = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(5, 5, 5, 5),
            };

            foreach (var part in _parts)
            {
                foreach (var inline in part.GetInlines(isInProgress))
                {
                    paragraph.Inlines.Add(inline);
                }

                var controlElement = acc?.GetCommandControls(part);
                if (controlElement is not null)
                {
                    paragraph.Inlines.Add(controlElement);
                }
            }

            return paragraph;
        }
    }
}
