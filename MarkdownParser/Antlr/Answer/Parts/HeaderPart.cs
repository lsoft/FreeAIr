using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>A `#`-`######` header part, rendered at a size scaled from its level via <see cref="IFontSizeProvider.GetHeaderFontSize"/>.</summary>
    public sealed class HeaderPart : IPart
    {
        /// <summary>Supplies the header font size for a given level, kept in sync with the chat's current text size setting.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;

        /// <summary>Identifies this part as a header for filtering by <see cref="PartTypeEnum"/>.</summary>
        public PartTypeEnum Type => PartTypeEnum.Header;

        /// <summary>The header level, 1 for `#` through 6 for `######`, as counted from the markdown's leading hashes.</summary>
        public int Level
        {
            get;
        }

        /// <summary>The raw inline markdown text of the header as parsed, including the leading `#` markers.</summary>
        public string Text
        {
            get;
        }
        /// <summary>The header text with the leading `#` markers trimmed off.</summary>
        public string Header
        {
            get;
        }

        /// <summary>Builds the part from the parsed level and header text, stripping the leading `#` markers into <see cref="Header"/>.</summary>
        public HeaderPart(
            IFontSizeProvider fontSizeProvider,
            int level,
            string header
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            _fontSizeProvider = fontSizeProvider;
            Level = level;
            Text = header;
            Header = header.TrimStart('#');
        }

        /// <summary>The parameter passed to an <see cref="AdditionalCommand"/> button for this part — the `#`-stripped header text.</summary>
        public object GetContextForAdditionalCommand()
        {
            return Header;
        }

        /// <summary>Renders the header text as a single <see cref="Run"/>, clamping levels beyond 6 to the smallest header size.</summary>
        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            var level = Level - 1;
            if (level >= 6)
            {
                level = 5;
            }

            yield return new Run
            {
                FontSize = _fontSizeProvider.GetHeaderFontSize(level),
                Text = Header
            };
        }
    }
}
