using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer
{
    public sealed class Block
    {
        private static readonly System.Windows.Media.Brush _semiTransparentGray = new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80));

        private readonly List<IPart> _parts = new();
        private readonly IFontSizeProvider _fontSizeProvider;
        private BlockUIContainer? _blockContainer;

        public BlockTypeEnum Type
        {
            get;
            private set;
        }

        public IReadOnlyList<IPart> Parts => _parts;

        public Block(
            IFontSizeProvider fontSizeProvider
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            _fontSizeProvider = fontSizeProvider;
            
            Type = BlockTypeEnum.Paragraph;
        }

        public void SetType(
            BlockTypeEnum type,
            BlockUIContainer? blockContainer
            )
        {
            Type = type;
            _blockContainer = blockContainer;
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

            var paragraph = new System.Windows.Documents.Paragraph
            {
                Margin = new Thickness(10, 0, 0, 0),
            };
            ModifyParagraph(paragraph);

            foreach (var part in Parts)
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

        private void ModifyParagraph(Paragraph paragraph)
        {
            switch (Type)
            {
                case BlockTypeEnum.Paragraph:
                    return;
                case BlockTypeEnum.Blockquote:
                    paragraph.Background = _semiTransparentGray;
                    paragraph.BorderBrush = Brushes.Green;
                    paragraph.BorderThickness = new Thickness(5, 0, 0, 0);
                    paragraph.Padding = new Thickness(5, 5, 5, 5);
                    return;
                case BlockTypeEnum.HorizontalRule:
                    return;
                default:
                    throw new InvalidOperationException(Type.ToString());
            }
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
