using MarkdownParser.Antlr.Answer;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WpfHelpers;

namespace MarkdownParser.UI.Dialog
{
    public partial class DialogControl : UserControl
    {
        private readonly object _locker = new();
        private bool _isScrolledToBottom = true;

        #region Dialog property

        public static readonly DependencyProperty DialogProperty =
            DependencyProperty.Register(
                nameof(Dialog),
                typeof(ObservableCollection<Replic>),
                typeof(DialogControl),
                new PropertyMetadata(OnDialogPropertyChanged));

        public ObservableCollection<Replic> Dialog
        {
            get => (ObservableCollection<Replic>)GetValue(DialogProperty);
            set
            {
                SetValue(DialogProperty, value);
                OnLastReplicChangedRaised();
            }
        }

        #region Dialog changes callbacks

        /// <summary>
        /// Обработчик изменения свойства Dialog
        /// </summary>
        private static void OnDialogPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DialogControl;
            if (e.OldValue is ObservableCollection<Replic> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnDialogCollectionChanged;

                // Отписываемся от старых элементов
                foreach (var replic in oldCollection)
                {
                    if (replic is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged -= control.OnReplicPropertyChanged;
                    }
                }
            }

            if (e.NewValue is ObservableCollection<Replic> newCollection)
            {
                newCollection.CollectionChanged += control.OnDialogCollectionChanged;

                // Подписываемся на новые элементы
                foreach (var replic in newCollection)
                {
                    if (replic is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged += control.OnReplicPropertyChanged;
                    }
                }
            }

            control.OnLastReplicChangedRaised();
        }

        /// <summary>
        /// Обработчик изменений коллекции
        /// </summary>
        private void OnDialogCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Replic replic in e.NewItems)
                {
                    if (replic is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged += OnReplicPropertyChanged;
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (Replic replic in e.OldItems)
                {
                    if (replic is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged -= OnReplicPropertyChanged;
                    }
                }
            }

            OnDialogUpdated();
        }

        /// <summary>
        /// Обработчик изменений конкретного Replic
        /// </summary>
        private void OnReplicPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Replic replic)
            {
                OnReplicChanged(replic);
            }
        }

        /// <summary>
        /// Callback при изменении коллекции
        /// </summary>
        private void OnDialogUpdated()
        {
            OnLastReplicChangedRaised();
        }

        /// <summary>
        /// Callback при изменении реплики
        /// </summary>
        private void OnReplicChanged(Replic replic)
        {
            OnLastReplicChangedRaised();
        }

        #endregion

        #endregion

        public DialogControl()
        {
            InitializeComponent();
        }

        private void OnLastReplicChangedRaised()
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

        public sealed class Replic : BaseViewModel
        {
            private readonly AdditionalCommandContainer? _acc;
            private FlowDocument _document;

            public ParsedAnswer ParsedAnswer
            {
                get;
                private set;
            }

            public object Tag
            {
                get;
            }

            public bool IsPrompt
            {
                get;
            }

            public HorizontalAlignment HorizontalAlignment
            {
                get;
            }

            public Thickness BorderThickness
            {
                get;
            }

            public FlowDocument Document
            {
                get => _document;
                private set
                {
                    _document = value;

                    OnPropertyChanged();
                }
            }

            public Replic(
                ParsedAnswer parsedAnswer,
                object tag,
                bool isPrompt,
                AdditionalCommandContainer? acc,
                bool inProgress
                )
            {
                _acc = acc;
                ParsedAnswer = parsedAnswer;
                Tag = tag;
                IsPrompt = isPrompt;
                HorizontalAlignment = isPrompt ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                BorderThickness = isPrompt ? new Thickness(1, 1, 10, 1) : new Thickness(10, 1, 1, 1);
                Document = parsedAnswer.ConvertToFlowDocument(_acc, inProgress);
            }

            public bool IsSameTag(object tag)
            {
                if (Tag is null && tag is null)
                {
                    return true;
                }
                if (tag is null && Tag is not null)
                {
                    return false;
                }
                if (Tag is null && tag is not null)
                {
                    return false;
                }

                return
                    ReferenceEquals(Tag, tag)
                    && Tag.GetType() == tag.GetType()
                    ;
            }

            public void Update(
                ParsedAnswer parsedAnswer,
                bool inProgress
                )
            {
                ParsedAnswer = parsedAnswer;
                Document = parsedAnswer.ConvertToFlowDocument(_acc, inProgress);
            }
        }
    }
}