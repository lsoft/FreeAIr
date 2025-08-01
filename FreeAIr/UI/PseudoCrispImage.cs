using FreeAIr.UI.Embedillo;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace FreeAIr.UI
{
    public sealed class PseudoCrispImage : System.Windows.Controls.Image
    {
        public static readonly DependencyProperty MonikerProperty =
            DependencyProperty.Register(
                nameof(Moniker),
                typeof(ImageMoniker),
                typeof(PseudoCrispImage),
                new PropertyMetadata(KnownMonikers.QuestionMark, OnKnownMonikerNameChanged)
                );

        public ImageMoniker Moniker
        {
            get => (ImageMoniker)GetValue(MonikerProperty);
            set => SetValue(MonikerProperty, value);
        }

        public PseudoCrispImage()
        {
            Width = 16;
            Height = 16;

            PseudoCrispImageThemeController.Add(this);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateImageSource();
        }

        private static void OnKnownMonikerNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PseudoCrispImage)d;
            control.UpdateImageSource();
        }

        public void UpdateImageSource()
        {
            var dpi = VisualTreeHelper.GetDpi(this);

            double realWidth = double.IsNaN(Width) ? 16 : Width;
            double realHeight = double.IsNaN(Height) ? 16 : Height;
            if (dpi.DpiScaleX > 0 && dpi.DpiScaleY > 0)
            {
                realWidth *= dpi.DpiScaleX;
                realHeight *= dpi.DpiScaleY;
            }

            Source = CompletionData.ConvertMonikerToImageSource(
                Moniker,
                (int)realWidth,
                (int)realHeight
                );
        }

        ~PseudoCrispImage()
        {
            PseudoCrispImageThemeController.Remove(this);
        }
    }

    public static class PseudoCrispImageThemeController
    {
        private static readonly object _locker = new();
        private static readonly List<WeakReference<PseudoCrispImage>> _references = new();

        static PseudoCrispImageThemeController()
        {
            VSColorTheme.ThemeChanged += VSThemeChanged;
        }

        public static void Add(PseudoCrispImage image)
        {
            lock (_locker)
            {
                _references.Add(
                    new WeakReference<PseudoCrispImage>(
                        image
                        )
                    );
            }
        }

        public static void Remove(PseudoCrispImage image)
        {
            lock (_locker)
            {
                _references.RemoveAll(wr =>
                {
                    var r = wr.TryGetTarget(out var target);
                    if (!r)
                    {
                        return true;
                    }

                    if (ReferenceEquals(target, image))
                    {
                        return true;
                    }

                    return false;
                });
            }
        }

        private static void VSThemeChanged(
            ThemeChangedEventArgs e
            )
        {
            var images = new List<PseudoCrispImage>();
            lock (_locker)
            {
                foreach (var wr in _references)
                {
                    if (wr.TryGetTarget(out var image))
                    {
                        images.Add(image);
                    }
                }
            }

            images.ForEach(image => image.UpdateImageSource());
        }
    }
}
