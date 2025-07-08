using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace FreeAIr.Antlr.Answer.Parts
{
    public sealed class ImagePart : IPart
    {
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

        public ImagePart(string text, string description, string link, string title)
        {
            Text = text;
            Description = description;
            Link = link;
            Title = title;
        }

        public object GetContextForAdditionalCommand()
        {
            var link = GetLink();

            return new BitmapImage(new Uri(link));

        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            if (isInProgress)
            {
                return [ new Run
                    {
                        FontSize = FontSizePage.Instance.TextSize,
                        Text = Text
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

                return [ new InlineUIContainer(image) ];
            }
            catch (Exception excp)
            {
                //image can not be found, for example; or other reason
                //todo log
            }

            return [];
        }

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
