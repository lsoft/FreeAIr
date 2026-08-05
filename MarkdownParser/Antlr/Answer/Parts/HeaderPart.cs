using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>A `#`-`######` header part, rendered at a size scaled from its level via <see cref="IFontSizeProvider.GetHeaderFontSize"/>.</summary>
    public sealed class HeaderPart : IPart
    {
        private readonly IFontSizeProvider _fontSizeProvider;

        public PartTypeEnum Type => PartTypeEnum.Header;

        public int Level
        {
            get;
        }

        public string Text
        {
            get;
        }
        public string Header
        {
            get;
        }

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
