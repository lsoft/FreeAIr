using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    public sealed class ParagraphBlock : IBlock, ITextualBlock
    {
        private readonly List<IPart> _parts = new();
        private readonly IFontSizeProvider _fontSizeProvider;
        private BlockUIContainer? _blockContainer;

        public BlockTypeEnum Type => BlockTypeEnum.Paragraph;

        public ParagraphBlock(
            IFontSizeProvider fontSizeProvider
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            _fontSizeProvider = fontSizeProvider;
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
                Margin = new Thickness(10, 10, 0, 0),
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

        public void AddXmlNode(string text, string nodeName, string body)
        {
            _parts.Add(new XmlNodePart(_fontSizeProvider, text, nodeName, body));
        }

        public void AddUrl(string text, string description, string link, string title)
        {
            _parts.Add(new UrlPart(_fontSizeProvider, text, description, link, title));
        }

        public void AddHeader(int headerLevel, string text)
        {
            _parts.Add(new HeaderPart(_fontSizeProvider, headerLevel, text));
        }

        public void AddCodeBlock(string text, string code)
        {
            _parts.Add(new CodeBlockPart(_fontSizeProvider, text, code));
        }

        public void AddCodeLine(string text)
        {
            _parts.Add(new CodeLinePart(_fontSizeProvider, text));
        }

        public void AddImage(string text, string description, string link, string title)
        {
            _parts.Add(new ImagePart(_fontSizeProvider, text, description, link, title));
        }
    }
}
