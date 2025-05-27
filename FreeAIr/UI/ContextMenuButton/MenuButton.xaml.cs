using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfHelpers;

namespace FreeAIr.UI.ContextMenuButton
{
    public partial class MenuButton : UserControl
    {
        public static readonly DependencyProperty ContextClickItemCommandProperty =
            DependencyProperty.Register(
                nameof(ContextClickItemCommand),
                typeof(ICommand),
                typeof(MenuButton)
                );

        public ObservableCollection2<ContextMenuItem> ContextItems
        {
            get;
        } = new();

        public ICommand ContextClickItemCommand
        {
            get => (ICommand)GetValue(ContextClickItemCommandProperty);
            set => SetValue(ContextClickItemCommandProperty, value);
        }

        public string QQ => "qq";

        public MenuButton()
        {
            InitializeComponent();
        }

        internal void SetButtonStyle(Style style)
        {
            TargetButton.Style = style;
            ForegroundButton.Style = style;
        }

        public void SetContent(FrameworkElement content)
        {
            TargetButton.Content = content;
        }
    }

    public class ContextMenuItem : BaseViewModel
    {
        public string Header
        {
            get;
            set;
        }

        public object Tag
        {
            get;
            set;
        }
    }

}
