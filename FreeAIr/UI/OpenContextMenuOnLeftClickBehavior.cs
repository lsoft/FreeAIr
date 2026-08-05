using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI
{
    /// <summary>
    /// XAML behavior that opens a button's own <see cref="ContextMenu"/> on a plain left click instead of
    /// requiring a right click, used for buttons that act as dropdown/menu triggers.
    /// </summary>
    public class OpenContextMenuOnLeftClickBehavior : Behavior<Button>
    {
        /// <summary>Subscribes to the associated button's click event when the behavior is attached.</summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += OnButtonClick;
        }

        /// <summary>Unsubscribes from the associated button's click event when the behavior is removed.</summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Click -= OnButtonClick;
        }

        /// <summary>Opens the button's context menu, anchored to the button itself, in response to a left click.</summary>
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.ContextMenu != null)
            {
                AssociatedObject.ContextMenu.PlacementTarget = AssociatedObject;
                AssociatedObject.ContextMenu.IsOpen = true;
            }
        }
    }
}
