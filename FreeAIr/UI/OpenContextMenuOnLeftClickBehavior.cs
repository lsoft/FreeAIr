using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI
{
    public class OpenContextMenuOnLeftClickBehavior : Behavior<Button>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += OnButtonClick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Click -= OnButtonClick;
        }

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
