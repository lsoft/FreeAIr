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
        private readonly IFontSizeProvider _fontSizeProvider;

        public PartTypeEnum Type => PartTypeEnum.Image;

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

        /// <summary>The parameter passed to an <see cref="AdditionalCommand"/> button for this part — the loaded image bitmap.</summary>
        public object GetContextForAdditionalCommand()
        {
            var link = GetLink();

            return new BitmapImage(new Uri(link));

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

                var link = GetLink();
                var bitmap = new BitmapImage(new Uri(link));
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
                //image can not be found, for example; or other reason
                //todo log
            }

            return [];
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
