using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.NestedCheckBox
{
    public sealed class SingleCheckedCheckableItem : CheckableItem
    {
        public SingleCheckedCheckableItem(
            string name,
            string description,
            bool isChecked,
            CheckableItemStyle style,
            object tag
            )
            : base(name, description, isChecked, style, tag)
        {
        }

        public SingleCheckedCheckableItem(
            string name,
            string description,
            CheckableItemStyle style,
            object tag,
            List<CheckableItem> children
            )
            : base(name, description, style, tag, children)
        {
        }

        protected override void Child_OnCheckedChanged(object sender, EventArgs e)
        {
            Children.ForEach(c => c.SetChecked(false));
            var child = sender as CheckableItem;
            child.SetChecked(true);
        }
    }

    public class CheckableItem : BaseViewModel
    {
        private bool? _isChecked;
        private readonly CheckableItemStyle _style;

        public string Name
        {
            get;
        }

        public string Description
        {
            get;
        }

        public Brush? Foreground
        {
            get
            {
                if (_style.UncheckedForeground is null)
                {
                    return _style.Foreground;
                }

                return IsChecked.GetValueOrDefault(false) ? _style.Foreground : _style.UncheckedForeground;
            }
        }

        public TextDecorationCollection TextDecoration => _style.TextDecoration;

        public bool IsEnabled => _style.IsEnabled;

        public object? Tag
        {
            get;
        }

        public ObservableCollection<CheckableItem> Children
        {
            get;
        }

        public event EventHandler? OnCheckedChangedEvent;

        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value)
                    return;

                _isChecked = value;

                // Обновляем всех детей при изменении родителя
                foreach (var child in Children)
                {
                    child.SetChecked(value);
                }

                OnPropertyChanged();
                FireCheckedChanged();
            }
        }

        public CheckableItem(
            string name,
            string description,
            bool isChecked,
            CheckableItemStyle style,
            object? tag
            )
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            _isChecked = isChecked;
            _style = style;
            Tag = tag;
            Children = new ObservableCollection<CheckableItem>();
        }

        public CheckableItem(
            string name,
            string description,
            CheckableItemStyle style,
            object? tag,
            List<CheckableItem> children
            )
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            _style = style;
            _isChecked = GetIsCheckedFromChildren(children);
            Tag = tag;
            Children = new ObservableCollection<CheckableItem>(children);

            foreach (var child in Children)
            {
                child.OnCheckedChangedEvent += Child_OnCheckedChanged;
            }
        }

        public void AddChild(
            CheckableItem child
            )
        {
            if (child is null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            Children.Add(child);
            child.OnCheckedChangedEvent += Child_OnCheckedChanged;
            UpdateCheckedStatusFromChildren();
        }

        public void SetChecked(bool? isChecked)
        {
            if (_isChecked == isChecked)
            {
                return;
            }

            _isChecked = isChecked;

            foreach (var child in Children)
            {
                child.SetChecked(isChecked);
            }

            OnPropertyChanged();
        }

        protected virtual void Child_OnCheckedChanged(object? sender, EventArgs e)
        {
            UpdateCheckedStatusFromChildren();
        }

        private void UpdateCheckedStatusFromChildren()
        {
            // Если все дети выбраны — родитель выбран
            // Если ни один — родитель не выбран
            // Если частично — в среднем состоянии
            // Если хотя бы один потомок в среднем состоянии - в среднем состоянии
            var isChecked = GetIsCheckedFromChildren(
                Children
                );

            if (_isChecked == isChecked)
            {
                return;
            }

            _isChecked = isChecked;
            FireCheckedChanged();
            OnPropertyChanged();
        }

        private static bool? GetIsCheckedFromChildren(
            IReadOnlyList<CheckableItem> children
            )
        {
            bool? isChecked;
            if (children.Count == 0)
            {
                isChecked = false;
            }
            else if (children.Any(c => !c.IsChecked.HasValue))
            {
                isChecked = null;
            }
            else if (children.All(c => c.IsChecked.GetValueOrDefault(false)))
            {
                isChecked = true;
            }
            else if (children.All(c => !c.IsChecked.GetValueOrDefault(true)))
            {
                isChecked = false;
            }
            else
            {
                isChecked = null;
            }

            return isChecked;
        }

        private void FireCheckedChanged()
        {
            OnCheckedChangedEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public readonly struct CheckableItemStyle
    {
        public static readonly CheckableItemStyle Empty = new CheckableItemStyle(null, null, true, new TextDecorationCollection());

        public readonly Brush? Foreground;
        public readonly Brush? UncheckedForeground;
        public readonly bool IsEnabled;
        public readonly TextDecorationCollection TextDecoration;

        public CheckableItemStyle(
            Brush foreground,
            Brush uncheckedForeground,
            bool isEnabled
            )
        {
            Foreground = foreground;
            UncheckedForeground = uncheckedForeground;
            IsEnabled = isEnabled;
            TextDecoration = new TextDecorationCollection();
        }

        public CheckableItemStyle(
            Brush foreground,
            Brush uncheckedForeground,
            bool isEnabled,
            TextDecorationCollection textDecoration
            )
        {
            Foreground = foreground;
            UncheckedForeground = uncheckedForeground;
            IsEnabled = isEnabled;
            TextDecoration = textDecoration;
        }
    }
}
