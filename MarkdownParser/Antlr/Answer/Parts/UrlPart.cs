using System.Diagnostics;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Parts
{
    public sealed class UrlPart : IPart
    {
        private readonly IFontSizeProvider _fontSizeProvider;

        public PartTypeEnum Type => PartTypeEnum.Url;

        public string Text
        {
            get;
        }

        public string Description
        {
            get;
        }

        public string Link
        {
            get;
        }

        public string Title
        {
            get;
        }

        public UrlPart(
            IFontSizeProvider fontSizeProvider,
            string text,
            string description,
            string link,
            string title
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

            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (link is null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            _fontSizeProvider = fontSizeProvider;
            Text = text;
            Description = description;
            Link = link;
            Title = string.IsNullOrEmpty(title) ? link : title;
        }

        public object GetContextForAdditionalCommand()
        {
            return Link;
        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            _ = Uri.TryCreate(Link, UriKind.RelativeOrAbsolute, out var uri);

            var hl = new Hyperlink(
                new Run(Description)
                )
            {
                FontSize = _fontSizeProvider.TextSize,
                NavigateUri = uri,
                ToolTip = Title
            };
            //PreviewMouseLeftButtonDown is to prevent FlowDocument scrolling when clicked
            hl.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                e.Handled = true;

                _ = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = Link,
                        UseShellExecute = true
                    }
                    );
            };
            yield return hl;
        }
    }
}
