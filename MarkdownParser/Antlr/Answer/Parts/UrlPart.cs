using System.Diagnostics;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>A `[desc](link "title")` markdown link part, rendered as a WPF <see cref="Hyperlink"/> that opens the link with the OS shell instead of navigating the flow document.</summary>
    public sealed class UrlPart : IPart
    {
        /// <summary>Supplies the font size used for the rendered <see cref="Hyperlink"/>, kept in sync with the chat's current text size setting.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;

        /// <summary>Identifies this part as a link/URL for filtering by <see cref="PartTypeEnum"/>.</summary>
        public PartTypeEnum Type => PartTypeEnum.Url;

        /// <summary>The raw inline markdown text of the link as parsed, e.g. `[desc](link "title")`.</summary>
        public string Text
        {
            get;
        }

        /// <summary>The visible link text shown to the user.</summary>
        public string Description
        {
            get;
        }

        /// <summary>The target URL the hyperlink navigates to when clicked.</summary>
        public string Link
        {
            get;
        }

        /// <summary>The hyperlink's tooltip text; falls back to <see cref="Link"/> when the markdown didn't supply a title.</summary>
        public string Title
        {
            get;
        }

        /// <summary>Builds the part from the parsed link pieces, defaulting <see cref="Title"/> to <see cref="Link"/> when no title was given.</summary>
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

        /// <summary>The parameter passed to an <see cref="AdditionalCommand"/> button for this part — the raw link.</summary>
        public object GetContextForAdditionalCommand()
        {
            return Link;
        }

        /// <summary>Builds a hyperlink that, on click, launches <see cref="Link"/> via the OS shell instead of letting the FlowDocument navigate/scroll.</summary>
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
