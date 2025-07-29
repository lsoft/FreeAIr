using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Parts
{
    public sealed class XmlNodePart : IPart
    {
        private readonly IFontSizeProvider _fontSizeProvider;

        public PartTypeEnum Type => PartTypeEnum.Xml;

        public string Text
        {
            get;
        }

        public string DefaultText
        {
            get;
        }

        public string VisibleText
        {
            get;
            private set;
        }

        public string NodeName
        {
            get;
        }

        public string Body
        {
            get;
        }
        public Run Run
        {
            get;
        }

        public XmlNodePart(
            IFontSizeProvider fontSizeProvider,
            string text,
            string nodeName,
            string body
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

            if (nodeName is null)
            {
                throw new ArgumentNullException(nameof(nodeName));
            }

            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            _fontSizeProvider = fontSizeProvider;
            Text = text;
            NodeName = nodeName;

            DefaultText = text;
            VisibleText =
                StringComparer.CurrentCultureIgnoreCase.Compare(nodeName, "think") == 0
                    ? GetCollapsedVisibleText()
                    : text
                    ;
            Body = body;

            Run = new Run
            {
                FontSize = _fontSizeProvider.TextSize,
                Foreground = Brushes.Red,
                Text = VisibleText
            };
        }

        public object GetContextForAdditionalCommand()
        {
            return this;
        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            yield return Run;
        }

        public void ExpandOrCollapse()
        {
            if (Run.Text == DefaultText)
            {
                //collapse
                Run.Text = GetCollapsedVisibleText();
            }
            else
            {
                Run.Text = DefaultText;
            }
        }

        private string GetCollapsedVisibleText()
        {
            return $"<{NodeName}>...</{NodeName}>";
        }
    }
}
