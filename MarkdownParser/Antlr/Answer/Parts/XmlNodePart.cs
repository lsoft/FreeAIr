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
        /// <summary>Supplies the font size used for the rendered <see cref="Run"/>, kept in sync with the chat's current text size setting.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;

        /// <summary>Identifies this part as an XML/tag node for filtering by <see cref="PartTypeEnum"/>.</summary>
        public PartTypeEnum Type => PartTypeEnum.Xml;

        /// <summary>The raw inline text of the node as parsed, e.g. `&lt;tag&gt;body&lt;/tag&gt;`.</summary>
        public string Text
        {
            get;
        }

        /// <summary>The node's full, uncollapsed text; used by <see cref="ExpandOrCollapse"/> to detect and restore the expanded state.</summary>
        public string DefaultText
        {
            get;
        }

        /// <summary>The text currently shown in <see cref="Run"/> — either <see cref="DefaultText"/> or the collapsed `&lt;tag&gt;...&lt;/tag&gt;` form.</summary>
        public string VisibleText
        {
            get;
            private set;
        }

        /// <summary>The tag name of the XML node, e.g. `think`, used to decide whether it starts collapsed.</summary>
        public string NodeName
        {
            get;
        }

        /// <summary>The text between the opening and closing tags of the node.</summary>
        public string Body
        {
            get;
        }

        /// <summary>The WPF <see cref="Run"/> rendered for this part, kept alive so <see cref="ExpandOrCollapse"/> can mutate its text after it has been placed in the document.</summary>
        public Run Run
        {
            get;
        }

        /// <summary>Builds the part from parsed text and tag pieces, and pre-collapses `&lt;think&gt;` nodes so reasoning output doesn't clutter the view by default.</summary>
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

        /// <summary>Builds the collapsed `&lt;tag&gt;...&lt;/tag&gt;` placeholder text for <see cref="NodeName"/>.</summary>
        private string GetCollapsedVisibleText()
        {
            return $"<{NodeName}>...</{NodeName}>";
        }
    }
}
