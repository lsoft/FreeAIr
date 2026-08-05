using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    /// <summary>A `&gt;` blockquote block, rendered as a single indented WPF <see cref="Paragraph"/> with a green left border.</summary>
    public sealed class BlockquoteBlock : IBlock, ITextualBlock
    {
        /// <summary>Background brush for the quote paragraph, a light gray at low opacity.</summary>
        private static readonly Brush _semiTransparentGray = new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80));

        /// <summary>The inline parts accumulated for this blockquote, in order.</summary>
        private readonly List<IPart> _parts = new();
        /// <summary>Supplies the font size used by text parts added via <see cref="AddText"/>.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;
        /// <summary>Cached WPF paragraph built by <see cref="CreateBlock"/>, reused on subsequent calls instead of rebuilding.</summary>
        private BlockUIContainer? _blockContainer;

        /// <summary>Always <see cref="BlockTypeEnum.Blockquote"/>.</summary>
        public BlockTypeEnum Type => BlockTypeEnum.Blockquote;

        /// <summary>Creates an empty blockquote block that will use <paramref name="fontSizeProvider"/> for its text parts.</summary>
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

        /// <summary>Appends text, merging into the trailing <see cref="TextPart"/> when possible instead of creating a new part.</summary>
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


        /// <summary>Builds (and caches) the WPF paragraph from all added parts, appending any matching command controls from <paramref name="acc"/>.</summary>
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
