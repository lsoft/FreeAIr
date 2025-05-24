using FreeAIr.UI.ViewModels;
using Microsoft.Xaml.Behaviors;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FreeAIr.UI.ToolWindows
{
    public partial class NaturalLanguageResultsToolWindowControl : UserControl
    {
        public NaturalLanguageResultsToolWindowControl(
            NaturalLanguageResultsViewModel viewModel
            )
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            InitializeComponent();

            this.DataContext = viewModel;
        }
    }

    public class ListViewGridViewBehavior : Behavior<ListView>
    {
        public static readonly DependencyProperty ColumnProportionsProperty =
            DependencyProperty.Register(
                nameof(ColumnProportions),
                typeof(DoubleCollection),
                typeof(ListViewGridViewBehavior),
                new PropertyMetadata(null, OnColumnProportionsChanged));

        public DoubleCollection ColumnProportions
        {
            get => (DoubleCollection)GetValue(ColumnProportionsProperty);
            set => SetValue(ColumnProportionsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.SizeChanged += OnSizeChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.SizeChanged -= OnSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _ = Dispatcher.BeginInvoke(new Action(UpdateColumns), DispatcherPriority.Loaded);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumns();
        }

        private void UpdateColumns()
        {
            if (AssociatedObject?.View is not GridView gridView)
                return;

            var scrollViewer = GetScrollViewer(AssociatedObject);
            double totalWidth = scrollViewer?.ViewportWidth ?? AssociatedObject.ActualWidth;

            if (totalWidth <= 0)
                return;

            var proportions = ColumnProportions ?? new DoubleCollection(gridView.Columns.Cast<object>().Select(_ => 1.0));
            double sum = proportions.Sum();

            for (int i = 0; i < gridView.Columns.Count; i++)
            {
                var column = gridView.Columns[i];
                double proportion = i < proportions.Count ? proportions[i] : 1.0;
                column.Width = (totalWidth / sum) * proportion;
            }
        }

        private static void OnColumnProportionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ListViewGridViewBehavior)d;
            behavior.UpdateColumns();
        }

        private static ScrollViewer GetScrollViewer(DependencyObject parent)
        {
            if (parent is ScrollViewer viewer)
                return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
