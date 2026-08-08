using System.Windows;
using System.Windows.Controls;

namespace WpfHelpers
{
    /// <summary>Attached-property behavior for a <see cref="ListBox"/>/<see cref="ListView"/> XAML binding: scrolls the newly-selected item into view whenever the bound selection changes.</summary>
    public static class ScrollToSelectedBehavior
    {
        public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.RegisterAttached(
            "SelectedValue",
            typeof(object),
            typeof(ScrollToSelectedBehavior),
            new PropertyMetadata(null, OnSelectedValueChange));

        /// <summary>WPF attached-property setter for <see cref="SelectedValueProperty"/>.</summary>
        public static void SetSelectedValue(DependencyObject source, object value)
        {
            source.SetValue(SelectedValueProperty, value);
        }

        /// <summary>WPF attached-property getter for <see cref="SelectedValueProperty"/>.</summary>
        public static object GetSelectedValue(DependencyObject source)
        {
            return (object)source.GetValue(SelectedValueProperty);
        }

        /// <summary>Scrolls the new selection into view on whichever of <see cref="ListBox"/>/<see cref="ListView"/> the property is attached to.</summary>
        private static void OnSelectedValueChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listbox = d as ListBox;
            if (listbox != null)
            {
                if (e.NewValue != null)
                {
                    listbox.ScrollIntoView(e.NewValue);
                }
                return;
            }

            var listview = d as ListView;
            if (listview != null)
            {
                if (e.NewValue != null)
                {
                    listview.ScrollIntoView(e.NewValue);
                }
                return;
            }
        }
    }
}
