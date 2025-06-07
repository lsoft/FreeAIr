using FreeAIr.Shared.Helper;
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

        public bool IsEnabled
        {
            get;
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
            object? tag,
            bool isEnabled
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
            IsEnabled = isEnabled;
        }

        public virtual void SetChecked(bool isChecked)
        {
            if (_isChecked == isChecked)
            {
                return;
            }

            HasChanged = true;
            _isChecked = isChecked;
            OnPropertyChanged();
        }
    }


    public sealed class SingleSelectedChildGroup : CheckableGroup
    {
        public SingleSelectedChildGroup(
            string name,
            string description,
            object? tag,
            IReadOnlyList<CheckableItem> children,
            bool isGroupEnabled
            ) : base(name, description, tag, children, isGroupEnabled)
        {
        }

        protected override void Child_OnCheckedChanged(object sender, EventArgs e)
        {
            Children.ForEach(c => c.SetChecked(false));
            var child = sender as CheckableItem;
            child.SetChecked(true);
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
            IReadOnlyList<CheckableItem> children,
            bool isGroupEnabled
            ) : base(name, description, children.All(c => c.IsChecked), tag, isGroupEnabled)
        {
            Children.AddRange(children);

            if (isGroupEnabled)
            {
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
            }

            //подписываемся на изменения каждого дочернего пункта
            foreach (var child in Children)
            {
                child.OnCheckedChanged += Child_OnCheckedChanged;
            }
        }

        protected virtual void Child_OnCheckedChanged(object sender, EventArgs e)
        {
            _isChecked = Children.All(c => c.IsChecked);
            OnPropertyChanged();
        }
    }

}
