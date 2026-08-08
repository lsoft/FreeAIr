using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Blocks
{
    /// <summary>
    /// A block of inline content — text, headers, links, images, XML nodes, code — rendered as a
    /// single WPF <see cref="Paragraph"/>. This is where most markdown parts end up, since
    /// <see cref="ParsedMarkdown"/> opens one whenever no more specific block applies.
    /// </summary>
    public sealed class ParagraphBlock : IBlock, ITextualBlock
    {
        /// <summary>The inline parts accumulated for this paragraph, in order.</summary>
        private readonly List<IPart> _parts = new();
        /// <summary>Supplies the font size used by parts added via the various Add* methods.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;
        /// <summary>Cached WPF paragraph built by <see cref="CreateBlock"/>, reused on subsequent calls instead of rebuilding.</summary>
        private BlockUIContainer? _blockContainer;

        /// <summary>Identifies this block as a paragraph for block-type dispatch.</summary>
        public BlockTypeEnum Type => BlockTypeEnum.Paragraph;

        /// <summary>Creates an empty paragraph block that will use <paramref name="fontSizeProvider"/> for its parts.</summary>
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

        /// <summary>Adds an inline XML node part.</summary>
        public void AddXmlNode(string text, string nodeName, string body)
        {
            _parts.Add(new XmlNodePart(_fontSizeProvider, text, nodeName, body));
        }

        /// <summary>Adds a URL/link part.</summary>
        public void AddUrl(string text, string description, string link, string title)
        {
            _parts.Add(new UrlPart(_fontSizeProvider, text, description, link, title));
        }

        /// <summary>Adds a header part.</summary>
        public void AddHeader(int headerLevel, string text)
        {
            _parts.Add(new HeaderPart(_fontSizeProvider, headerLevel, text));
        }

        /// <summary>Adds a fenced code block part.</summary>
        public void AddCodeBlock(string text, string code)
        {
            _parts.Add(new CodeBlockPart(_fontSizeProvider, text, code));
        }

        /// <summary>Adds a single code-line part.</summary>
        public void AddCodeLine(string text)
        {
            _parts.Add(new CodeLinePart(_fontSizeProvider, text));
        }

        /// <summary>Adds an image part.</summary>
        public void AddImage(string text, string description, string link, string title)
        {
            _parts.Add(new ImagePart(_fontSizeProvider, text, description, link, title));
        }
    }
}
