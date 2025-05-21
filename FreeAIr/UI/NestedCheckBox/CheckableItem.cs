using System.Collections.Generic;
using System.Linq;
using WpfHelpers;

namespace FreeAIr.UI.NestedCheckBox
{
    public class CheckableItem : BaseViewModel
    {
        protected bool _isChecked;

        public event EventHandler? OnCheckedChanged;

        public string Name
        {
            get;
        }

        public string Description
        {
            get;
        }
        
        public object? Tag
        {
            get;
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value)
                {
                    return;
                }

                HasChanged = true;
                _isChecked = value;
                OnPropertyChanged();
                OnCheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool HasChanged
        {
            get;
            private set;
        }

        public CheckableItem(
            string name,
            string description,
            bool isChecked,
            object? tag
            )
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            Name = name;
            Description = description;
            _isChecked = isChecked;
            Tag = tag;
        }

        public void SetChecked(bool isChecked)
        {
            _isChecked = isChecked;
            OnPropertyChanged();
        }
    }

    public class CheckableGroup : CheckableItem
    {
        public ObservableCollection2<CheckableItem> Children
        {
            get;
        } = new();

        public CheckableGroup(
            string name,
            string description,
            object? tag,
            IReadOnlyList<CheckableItem> children
            ) : base(name, description, children.All(c => c.IsChecked), tag)
        {
            Children.AddRange(children);

            //когда изменяется состояние группы — обновляем всех детей
            this.OnCheckedChanged += (sender, args) =>
            {
                if (sender is not CheckableGroup group)
                {
                    return;
                }

                foreach (var child in Children)
                {
                    child.SetChecked(
                        group.IsChecked
                        );
                }
            };

            //подписываемся на изменения каждого дочернего пункта
            foreach (var child in Children)
            {
                child.OnCheckedChanged += (sender, args) =>
                {
                    _isChecked = Children.All(c => c.IsChecked);
                    OnPropertyChanged();
                };
            }
        }
    }

}
