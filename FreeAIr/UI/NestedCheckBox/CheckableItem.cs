using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            Brush? foreground,
            object tag
            )
            : base(name, description, isChecked, foreground, tag)
        {
        }

        public SingleCheckedCheckableItem(
            string name,
            string description,
            Brush? foreground,
            object tag,
            List<CheckableItem> children
            )
            : base(name, description, foreground, tag, children)
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
        private readonly Brush _foreground;

        public string Name
        {
            get;
        }

        public string Description
        {
            get;
        }

        public Brush? Foreground => _foreground;

        public Brush? Foreground2 => Brushes.Blue;

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
                OnPropertyChanged();
                FireCheckedChanged();

                // Обновляем всех детей при изменении родителя
                foreach (var child in Children)
                {
                    child.SetChecked(value);
                }
            }
        }

        public CheckableItem(
            string name,
            string description,
            bool isChecked,
            Brush? foreground,
            object? tag
            )
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            _isChecked = isChecked;
            _foreground = foreground;
            Tag = tag;
            Children = new ObservableCollection<CheckableItem>();
        }

        public CheckableItem(
            string name,
            string description,
            Brush? foreground,
            object? tag,
            List<CheckableItem> children
            )
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            _foreground = foreground;
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
            OnPropertyChanged();

            foreach (var child in Children)
            {
                child.SetChecked(isChecked);
            }
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
            OnPropertyChanged(nameof(IsChecked));
        }

        private void FireCheckedChanged()
        {
            OnCheckedChangedEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
