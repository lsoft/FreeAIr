using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;

namespace FreeAIr.UI
{
    /// <summary>
    /// Attached-property helper that lets any WPF control opt into the Visual Studio shell theme (colors,
    /// scrollbar and textbox styles) via <c>VsTheme.UseVsTheme="True"</c> in XAML, instead of using its own
    /// hard-coded WPF styling.
    /// </summary>
    public static class VsTheme
    {
        /// <summary>
        /// Tracks, per element, whether the VS theme is currently applied to it.
        /// </summary>
        private static readonly Dictionary<UIElement, bool> _isUsingVsTheme = new Dictionary<UIElement, bool>();

        /// <summary>
        /// Remembers each themed control's background as it was before theming, so it can be restored when
        /// theming is turned off.
        /// </summary>
        private static readonly Dictionary<UIElement, object> _originalBackgrounds = new Dictionary<UIElement, object>();

        /// <summary>
        /// Attached dependency property that toggles VS shell theming for the element it is set on.
        /// </summary>
        public static DependencyProperty UseVsThemeProperty = DependencyProperty.RegisterAttached("UseVsTheme", typeof(bool), typeof(VsTheme), new PropertyMetadata(false, UseVsThemePropertyChanged));

        /// <summary>
        /// Callback invoked when <see cref="UseVsThemeProperty"/> changes on an element, applying or removing
        /// the VS theme accordingly.
        /// </summary>
        private static void UseVsThemePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetUseVsTheme((FrameworkElement)d, (bool)e.NewValue);
        }

        /// <summary>
        /// Turns VS shell theming on or off for the given element, merging in (or removing) the shared theme
        /// resource dictionary and swapping its background brush accordingly.
        /// </summary>
        public static void SetUseVsTheme(FrameworkElement element, bool value)
        {
            if (value)
            {
                if (!_originalBackgrounds.ContainsKey(element) && element is System.Windows.Controls.Control c)
                {
                    _originalBackgrounds[element] = c.Background;
                }

                element.ShouldBeThemed();
            }
            else
            {
                element.ShouldNotBeThemed();
            }

            _isUsingVsTheme[element] = value;
        }

        /// <summary>
        /// Returns whether VS shell theming is currently applied to the given element.
        /// </summary>
        public static bool GetUseVsTheme(UIElement element)
        {
            return _isUsingVsTheme.TryGetValue(element, out var value) && value;
        }

        /// <summary>
        /// Loads the Visual Studio shell's dialog and scrollbar resource dictionaries and derives the
        /// TextBox/ComboBox/ScrollViewer styles used to theme extension controls, falling back to an empty
        /// dictionary if the shell resources cannot be loaded (e.g. outside a running VS instance).
        /// </summary>
        private static ResourceDictionary BuildThemeResources()
        {
            var allResources = new ResourceDictionary();

            try
            {
                var shellResources = (ResourceDictionary)Application.LoadComponent(new Uri("Microsoft.VisualStudio.Platform.WindowManagement;component/Themes/ThemedDialogDefaultStyles.xaml", UriKind.Relative));
                var scrollStyleContainer = (ResourceDictionary)Application.LoadComponent(new Uri("Microsoft.VisualStudio.Shell.UI.Internal;component/Styles/ScrollBarStyle.xaml", UriKind.Relative));
                allResources.MergedDictionaries.Add(shellResources);
                allResources.MergedDictionaries.Add(scrollStyleContainer);
                allResources[typeof(ScrollViewer)] = new Style
                {
                    TargetType = typeof(ScrollViewer),
                    BasedOn = (Style)scrollStyleContainer[VsResourceKeys.ScrollViewerStyleKey]
                };

                allResources[typeof(TextBox)] = new Style
                {
                    TargetType = typeof(TextBox),
                    BasedOn = (Style)shellResources[typeof(TextBox)],
                    Setters =
                    {
                        new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(2, 3, 2, 3))
                    }
                };

                allResources[typeof(ComboBox)] = new Style
                {
                    TargetType = typeof(ComboBox),
                    BasedOn = (Style)shellResources[typeof(ComboBox)],
                    Setters =
                    {
                        new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(2, 3, 2, 3))
                    }
                };
            }
            catch
            {
            }

            return allResources;
        }

        /// <summary>
        /// The shared, lazily-built VS shell resource dictionary merged into every themed control.
        /// </summary>
        private static ResourceDictionary ThemeResources { get; } = BuildThemeResources();

        /// <summary>
        /// Merges the shared VS theme resources into the control's resource dictionary and switches its
        /// background to the VS shell's start-page tab background brush.
        /// </summary>
        private static void ShouldBeThemed(this FrameworkElement control)
        {
            if (control.Resources == null)
            {
                control.Resources = ThemeResources;
            }
            else if (control.Resources != ThemeResources)
            {
                var d = new ResourceDictionary();
                d.MergedDictionaries.Add(ThemeResources);
                d.MergedDictionaries.Add(control.Resources);
                control.Resources = null;
                control.Resources = d;
            }

            if (control is System.Windows.Controls.Control c)
            {
                c.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, (string)EnvironmentColors.StartPageTabBackgroundBrushKey);
            }
        }

        /// <summary>
        /// Removes the shared VS theme resources from the control and restores its original background
        /// (or clears it if none was recorded), undoing what <see cref="ShouldBeThemed"/> applied.
        /// </summary>
        private static void ShouldNotBeThemed(this FrameworkElement control)
        {
            if (control.Resources != null)
            {
                if (control.Resources == ThemeResources)
                {
                    control.Resources = new ResourceDictionary();
                }
                else
                {
                    control.Resources.MergedDictionaries.Remove(ThemeResources);
                }
            }

            //If we're themed now and we're something with a background property, reset it
            if (GetUseVsTheme(control) && control is System.Windows.Controls.Control c)
            {
                if (_originalBackgrounds.TryGetValue(control, out var background))
                {
                    c.SetValue(System.Windows.Controls.Control.BackgroundProperty, background);
                }
                else
                {
                    c.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                }
            }
        }
    }
}
