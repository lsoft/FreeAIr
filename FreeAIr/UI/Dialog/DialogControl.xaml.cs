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
    public partial class DialogControl : UserControl
    {
        private readonly object _locker = new();
        private bool _isScrolledToBottom = true;

        #region Dialog property

        public static readonly DependencyProperty DialogProperty =
            DependencyProperty.Register(
                nameof(Dialog),
                typeof(ObservableCollection<DialogContent>),
                typeof(DialogControl),
                new PropertyMetadata(OnDialogPropertyChanged));

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

        public DialogControl()
        {
            InitializeComponent();
        }

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

        private void FlowDocumentScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var flowDocumentViewer = sender as FlowDocumentScrollViewer;
            if (flowDocumentViewer is null)
            {
                return;
            }

            flowDocumentViewer.Document.PageWidth = DialogControlName.ActualWidth * 0.75;
        }

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