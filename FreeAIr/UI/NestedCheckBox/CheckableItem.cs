using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
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
        private bool _isChecked;
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
                if (_style.DisabledForeground is null)
                {
                    return _style.Foreground;
                }

                return IsChecked ? _style.Foreground : _style.DisabledForeground;
            }
        }

        public TextDecorationCollection TextDecoration => _style.TextDecoration;

        public object? Tag
        {
            get;
        }

        public ObservableCollection<CheckableItem> Children
        {
            get;
        }

        public event EventHandler? OnCheckedChangedEvent;

        public bool IsChecked
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
            _isChecked = children.Any(c => c.IsChecked);
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

        public void SetChecked(bool isChecked)
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
            // Если частично — можно сделать дополнительную логику, например IsThreeState (необязательно)
            var isChecked = Children.Any(c => c.IsChecked);
            if (_isChecked == isChecked)
            {
                return;
            }

            _isChecked = isChecked;
            FireCheckedChanged();
            OnPropertyChanged();
        }

        private void FireCheckedChanged()
        {
            OnCheckedChangedEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public readonly struct CheckableItemStyle
    {
        public static readonly CheckableItemStyle Empty = new CheckableItemStyle(null, null, new TextDecorationCollection());

        public readonly Brush? Foreground;
        public readonly Brush? DisabledForeground;
        public readonly TextDecorationCollection TextDecoration;

        public CheckableItemStyle(
            Brush foreground,
            Brush disabledForeground
            )
        {
            Foreground = foreground;
            DisabledForeground = disabledForeground;
            TextDecoration = new TextDecorationCollection();
        }

        public CheckableItemStyle(
            Brush foreground,
            Brush disabledForeground,
            TextDecorationCollection textDecoration
            )
        {
            Foreground = foreground;
            DisabledForeground = disabledForeground;
            TextDecoration = textDecoration;
        }
    }
}
