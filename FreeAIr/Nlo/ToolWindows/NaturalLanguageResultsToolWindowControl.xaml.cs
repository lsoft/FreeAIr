using FreeAIr.UI.ViewModels;
using Microsoft.Xaml.Behaviors;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// Code-behind for the natural language search results list; wires the supplied view model
    /// as the control's data context for the XAML bindings.
    /// </summary>
    public partial class NaturalLanguageResultsToolWindowControl : UserControl
    {
        /// <summary>
        /// Creates the control and binds it to the search results view model.
        /// </summary>
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

    /// <summary>
    /// WPF behavior that keeps a <see cref="ListView"/>'s <see cref="GridView"/> columns sized as
    /// fixed proportions of the available width, so the search results grid resizes cleanly
    /// instead of leaving columns at their initial fixed widths.
    /// </summary>
    public class ListViewGridViewBehavior : Behavior<ListView>
    {
        /// <summary>
        /// Backing dependency property for <see cref="ColumnProportions"/>.
        /// </summary>
        public static readonly DependencyProperty ColumnProportionsProperty =
            DependencyProperty.Register(
                nameof(ColumnProportions),
                typeof(DoubleCollection),
                typeof(ListViewGridViewBehavior),
                new PropertyMetadata(null, OnColumnProportionsChanged));

        /// <summary>
        /// The relative width weights assigned to each grid column; columns are sized so their
        /// widths stay in this proportion as the list view is resized.
        /// </summary>
        public DoubleCollection ColumnProportions
        {
            get => (DoubleCollection)GetValue(ColumnProportionsProperty);
            set => SetValue(ColumnProportionsProperty, value);
        }

        /// <summary>
        /// Subscribes to the list view's load and resize events so columns can be recalculated.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.SizeChanged += OnSizeChanged;
        }

        /// <summary>
        /// Unsubscribes from the list view's load and resize events.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.SizeChanged -= OnSizeChanged;
        }

        /// <summary>
        /// Recalculates column widths once the list view has finished loading and its actual
        /// viewport size is known.
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _ = Dispatcher.BeginInvoke(new Action(UpdateColumns), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Recalculates column widths whenever the list view is resized.
        /// </summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumns();
        }

        /// <summary>
        /// Distributes the list view's current viewport width across the grid columns according
        /// to <see cref="ColumnProportions"/>, falling back to equal widths when none are set.
        /// </summary>
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

        /// <summary>
        /// Reapplies column widths whenever <see cref="ColumnProportions"/> changes.
        /// </summary>
        private static void OnColumnProportionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ListViewGridViewBehavior)d;
            behavior.UpdateColumns();
        }

        /// <summary>
        /// Walks the visual tree to find the <see cref="ScrollViewer"/> hosting the list view, so
        /// its viewport width (rather than the possibly-stale actual width) drives column sizing.
        /// </summary>
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
