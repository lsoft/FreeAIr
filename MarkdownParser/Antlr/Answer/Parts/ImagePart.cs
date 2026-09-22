using System.IO;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace MarkdownParser.Antlr.Answer.Parts
{
    /// <summary>
    /// A `![desc](link "title")` image part. While the answer is still streaming
    /// (<paramref name="isInProgress"/> in <see cref="GetInlines"/>) it renders as placeholder text
    /// instead of loading the image, since the link may still be incomplete.
    /// </summary>
    public sealed class ImagePart : IPart
    {
        /// <summary>Supplies the font size used for the streaming placeholder text.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;

        /// <summary>Identifies this part as an image for part-type dispatch.</summary>
        public PartTypeEnum Type => PartTypeEnum.Image;

        /// <summary>The raw matched markdown text for this image, shown as a placeholder while streaming.</summary>
        public string Text
        {
            get;
        }

        /// <summary>The image's alt/description text from `![description](...)`.</summary>
        public string Description
        {
            get;
        }

        /// <summary>The image source, either a local `/`-rooted path or a web URL.</summary>
        public string Link
        {
            get;
        }

        /// <summary>The optional tooltip title from `![desc](link "title")`.</summary>
        public string Title
        {
            get;
        }

        /// <summary>Creates an image part from its parsed markdown pieces.</summary>
        public ImagePart(
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

            _fontSizeProvider = fontSizeProvider;
            Text = text;
            Description = description;
            Link = link;
            Title = title;
        }

        /// <summary>
        /// The parameter passed to an <see cref="AdditionalCommand"/> button for this part — the
        /// loaded image bitmap, or null when the link cannot be loaded. Null rather than an
        /// exception because an answer's markdown routinely names an image which is not there, and
        /// the copy-to-clipboard button is not worth the whole chat window (issue #73).
        /// </summary>
        public object? GetContextForAdditionalCommand()
        {
            return TryLoadBitmap();
        }

        /// <summary>While streaming, yields placeholder text; otherwise loads the image (async for 1x1 web-placeholder bitmaps) or, on failure, yields nothing.</summary>
        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            if (isInProgress)
            {
                return [ new Run
                    {
                        FontSize = _fontSizeProvider.TextSize,
                        Text = Text,
                        ToolTip = Title
                    }
                ];
            }

            try
            {
                var image = new System.Windows.Controls.Image
                {
                };

                var bitmap = TryLoadBitmap();
                if (bitmap is null)
                {
                    return [];
                }

                if (bitmap.Width == 1 && bitmap.Height == 1) //for web images
                {
                    bitmap.DownloadCompleted += (sender, e) =>
                    {
                        image.Source = bitmap;
                        image.Width = bitmap.Width;
                        image.Height = bitmap.Height;
                    };
                }
                else //for local images
                {
                    image.Source = bitmap;
                    image.Width = bitmap.Width;
                    image.Height = bitmap.Height;
                }

                image.Source = bitmap;
                image.ToolTip = Title;

                return [ new InlineUIContainer(image) ];
            }
            catch (Exception excp)
            {
                //the bitmap loaded but could not be shown - a broken frame, say
                WpfHelpers.CommandDiagnostics.Report(excp);
            }

            return [];
        }

        /// <summary>
        /// Loads the image, answering null instead of throwing for the two things an answer's
        /// markdown routinely contains: a link which is no absolute uri at all — `![x](diagram.png)`,
        /// `![x](./img/a.svg)`, `![x](#anchor)` are all accepted by the IMAGE lexer rule — and one
        /// which is, but names nothing readable. <see cref="BitmapImage"/> opens its source right in
        /// the constructor, so a missing file throws here rather than at rendering time.
        /// </summary>
        private BitmapImage? TryLoadBitmap()
        {
            var link = GetLink();

            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
            {
                return null;
            }

            try
            {
                return new BitmapImage(uri);
            }
            catch (Exception excp)
            {
                //no such file, unreadable, or not an image format WPF decodes
                WpfHelpers.CommandDiagnostics.Report(excp);
                return null;
            }
        }

        /// <summary>Resolves a `/`-rooted link relative to the current directory; leaves absolute/web links untouched.</summary>
        private string GetLink()
        {
            string link;
            if (Link.StartsWith("/"))
            {
                link = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    Link.Substring(1)
                    );
            }
            else
            {
                link = Link;
            }

            return link;
        }

    }
}
