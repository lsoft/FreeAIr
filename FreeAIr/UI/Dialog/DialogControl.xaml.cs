using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.Dialog
{
    /// <summary>
    /// The scrollable chat transcript control: renders the bound <see cref="Dialog"/> collection of prompts
    /// and answers, auto-scrolls to the newest message while the user is at the bottom, and stops
    /// auto-scrolling once the user scrolls up to read earlier messages.
    /// </summary>
    public partial class DialogControl : UserControl
    {
        /// <summary>
        /// Guards <see cref="_isScrolledToBottom"/> and the auto-scroll check against concurrent access from
        /// UI events and dialog-collection change callbacks.
        /// </summary>
        private readonly object _locker = new();

        /// <summary>
        /// Whether the transcript view is currently scrolled to the bottom; auto-scroll on new content only
        /// happens while this is true, so reading history is not interrupted by incoming messages.
        /// </summary>
        private bool _isScrolledToBottom = true;

        #region Dialog property

        /// <summary>
        /// Backing dependency property for <see cref="Dialog"/>, the bound collection of chat messages shown
        /// in the transcript.
        /// </summary>
        public static readonly DependencyProperty DialogProperty =
            DependencyProperty.Register(
                nameof(Dialog),
                typeof(ObservableCollection<DialogContent>),
                typeof(DialogControl),
                new PropertyMetadata(OnDialogPropertyChanged));

        /// <summary>
        /// The collection of chat messages (prompts and answers) rendered by this control.
        /// </summary>
        public ObservableCollection<DialogContent> Dialog
        {
            get => (ObservableCollection<DialogContent>)GetValue(DialogProperty);
            set
            {
                SetValue(DialogProperty, value);
                OnLastContentChangedRaised();
            }
        }

        #region Dialog changes callbacks

        /// <summary>
        /// Обработчик изменения свойства Dialog
        /// </summary>
        private static void OnDialogPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DialogControl;
            if (e.OldValue is ObservableCollection<DialogContent> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnDialogCollectionChanged;

                // Отписываемся от старых элементов
                foreach (var content in oldCollection)
                {
                    if (content is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged -= control.OnContentPropertyChanged;
                    }
                }
            }

            if (e.NewValue is ObservableCollection<DialogContent> newCollection)
            {
                newCollection.CollectionChanged += control.OnDialogCollectionChanged;

                // Подписываемся на новые элементы
                foreach (var content in newCollection)
                {
                    if (content is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged += control.OnContentPropertyChanged;
                    }
                }
            }

            control.OnLastContentChangedRaised();
        }

        /// <summary>
        /// Обработчик изменений коллекции
        /// </summary>
        private void OnDialogCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DialogContent content in e.NewItems)
                {
                    if (content is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged += OnContentPropertyChanged;
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (DialogContent content in e.OldItems)
                {
                    if (content is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged -= OnContentPropertyChanged;
                    }
                }
            }

            OnDialogUpdated();
        }

        /// <summary>
        /// Обработчик изменений конкретного контента (например, промпта, или ответа LLM)
        /// </summary>
        private void OnContentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is DialogContent content)
            {
                OnContentChanged(content);
            }
        }

        /// <summary>
        /// Callback при изменении коллекции
        /// </summary>
        private void OnDialogUpdated()
        {
            OnLastContentChangedRaised();
        }

        /// <summary>
        /// Callback при изменении реплики
        /// </summary>
        private void OnContentChanged(DialogContent content)
        {
            OnLastContentChangedRaised();
        }

        #endregion

        #endregion

        /// <summary>
        /// Creates the dialog transcript control and loads its XAML visual tree.
        /// </summary>
        public DialogControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Scrolls the transcript to the bottom in response to a new or updated message, but only when the
        /// user was already at the bottom, so it never yanks the view away while reading older messages.
        /// </summary>
        private void OnLastContentChangedRaised()
        {
            var locked = false;
            try
            {
                Monitor.TryEnter(_locker, ref locked);
                if (locked)
                {
                    if (_isScrolledToBottom)
                    {
                        ScrollViewerName.ScrollToBottom();
                    }
                }
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(_locker);
                }
            }
        }

        /// <summary>
        /// Handles manual mouse-wheel scrolling in the transcript by scrolling the ancestor
        /// <see cref="ScrollViewer"/> and recording whether the user has scrolled away from the bottom, so
        /// subsequent auto-scroll decisions respect that choice.
        /// </summary>
        private void OnFlowDocumentScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            lock (_locker)
            {
                var scrollViewer = FindAncestor<ScrollViewer>(sender as DependencyObject);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                    _isScrolledToBottom = ScrollViewerName.IsScrolledToBottom();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Keeps each message's flow document sized relative to the transcript width (75% of it) as the
        /// control is resized, so long lines wrap at a readable width instead of stretching edge to edge.
        /// </summary>
        private void FlowDocumentScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var flowDocumentViewer = sender as FlowDocumentScrollViewer;
            if (flowDocumentViewer is null)
            {
                return;
            }

            flowDocumentViewer.Document.PageWidth = DialogControlName.ActualWidth * 0.75;
        }

        /// <summary>
        /// Walks up the WPF visual tree from <paramref name="current"/> and returns the nearest ancestor of
        /// type <typeparamref name="T"/>, or null if none is found; used to locate the enclosing
        /// <see cref="ScrollViewer"/> from a mouse-wheel event's source element.
        /// </summary>
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);

            return null;
        }
    }
}