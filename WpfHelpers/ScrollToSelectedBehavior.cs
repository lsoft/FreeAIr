using System.Windows;
using System.Windows.Controls;

namespace WpfHelpers
{
    public static class ScrollToSelectedBehavior
    {
        public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.RegisterAttached(
            "SelectedValue",
            typeof(object),
            typeof(ScrollToSelectedBehavior),
            new PropertyMetadata(null, OnSelectedValueChange));

        public static void SetSelectedValue(DependencyObject source, object value)
        {
            source.SetValue(SelectedValueProperty, value);
        }

        public static object GetSelectedValue(DependencyObject source)
        {
            return (object)source.GetValue(SelectedValueProperty);
        }

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
