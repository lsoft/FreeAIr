using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>
    /// An inline `&lt;tag&gt;body&lt;/tag&gt;` part. A `&lt;think&gt;` node starts collapsed to
    /// `&lt;think&gt;...&lt;/think&gt;` since it carries reasoning output the user usually doesn't
    /// need to read; <see cref="ExpandOrCollapse"/> toggles it on click.
    /// </summary>
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

        /// <summary>The parameter passed to an <see cref="AdditionalCommand"/> button for this part — the part itself, so a command can call <see cref="ExpandOrCollapse"/>.</summary>
        public object GetContextForAdditionalCommand()
        {
            return this;
        }

        /// <summary>Yields the shared, mutable <see cref="Run"/> so later toggling by <see cref="ExpandOrCollapse"/> updates the already-rendered document.</summary>
        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            yield return Run;
        }

        /// <summary>Toggles the node's <see cref="Run"/> between its full text and the collapsed `&lt;tag&gt;...&lt;/tag&gt;` form.</summary>
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
