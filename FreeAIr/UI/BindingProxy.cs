using System.Windows;

namespace FreeAIr.UI
{
    /// <summary>
    /// WPF freezable used to smuggle a DataContext into elements (e.g. context menus, popups) that XAML
    /// places outside the visual tree and therefore cannot bind to it directly.
    /// </summary>
    public class BindingProxy : Freezable
    {
        /// <summary>Required by <see cref="Freezable"/>; creates a new unfrozen instance for the WPF cloning machinery.</summary>
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        /// <summary>Backing dependency property for <see cref="Data"/>.</summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            "_cached",
            typeof(object),
            typeof(BindingProxy),
            new UIPropertyMetadata(null)
            );

        /// <summary>The proxied value, typically bound to a DataContext that would otherwise be unreachable.</summary>
        public object Data
        {
            get
            {
                return GetValue(DataProperty);
            }
            set
            {
                SetValue(DataProperty, value);
            }
        }
    }
}
