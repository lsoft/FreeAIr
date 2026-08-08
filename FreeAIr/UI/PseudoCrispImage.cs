using FreeAIr.UI.Embedillo;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace FreeAIr.UI
{
    /// <summary>
    /// WPF Image control that renders a Visual Studio <see cref="ImageMoniker"/> at the current DPI and
    /// theme, re-rendering itself whenever its size or the VS color theme changes so glyphs stay crisp
    /// instead of being blurrily upscaled by WPF's default image stretching.
    /// </summary>
    public sealed class PseudoCrispImage : System.Windows.Controls.Image
    {
        /// <summary>Backing dependency property for <see cref="Moniker"/>.</summary>
        public static readonly DependencyProperty MonikerProperty =
            DependencyProperty.Register(
                nameof(Moniker),
                typeof(ImageMoniker),
                typeof(PseudoCrispImage),
                new PropertyMetadata(KnownMonikers.QuestionMark, OnKnownMonikerNameChanged)
                );

        /// <summary>The Visual Studio image moniker to render, e.g. a toolbar or context-menu glyph.</summary>
        public ImageMoniker Moniker
        {
            get => (ImageMoniker)GetValue(MonikerProperty);
            set => SetValue(MonikerProperty, value);
        }

        /// <summary>Creates the control with the default 16x16 size and registers it for theme-change notifications.</summary>
        public PseudoCrispImage()
        {
            Width = 16;
            Height = 16;

            PseudoCrispImageThemeController.Add(this);
        }

        /// <summary>Re-renders the moniker at the new size whenever the control is resized.</summary>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateImageSource();
        }

        /// <summary>Re-renders the image whenever the <see cref="Moniker"/> dependency property changes.</summary>
        private static void OnKnownMonikerNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PseudoCrispImage)d;
            control.UpdateImageSource();
        }

        /// <summary>
        /// Converts the current <see cref="Moniker"/> into a DPI-aware bitmap sized for this control's
        /// actual dimensions, so it stays sharp instead of being stretched by WPF.
        /// </summary>
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

        /// <summary>Unregisters this instance from theme-change notifications when it is collected.</summary>
        ~PseudoCrispImage()
        {
            PseudoCrispImageThemeController.Remove(this);
        }
    }

    /// <summary>
    /// Keeps weak references to every live <see cref="PseudoCrispImage"/> and asks each one to
    /// re-render its moniker whenever the Visual Studio color theme changes, so glyphs pick up the
    /// new theme's colors without leaking the controls themselves.
    /// </summary>
    public static class PseudoCrispImageThemeController
    {
        /// <summary>Guards access to <see cref="_references"/> from the theme-changed event and UI thread.</summary>
        private static readonly object _locker = new();
        /// <summary>Weak references to every tracked <see cref="PseudoCrispImage"/>, so the controller does not keep them alive.</summary>
        private static readonly List<WeakReference<PseudoCrispImage>> _references = new();

        static PseudoCrispImageThemeController()
        {
            VSColorTheme.ThemeChanged += VSThemeChanged;
        }

        /// <summary>Starts tracking an image control so it is refreshed on the next theme change.</summary>
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

        /// <summary>Stops tracking an image control (and prunes any dead weak references found along the way).</summary>
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

        /// <summary>Refreshes the image source of every live tracked control after the VS color theme changes.</summary>
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
