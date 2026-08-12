using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.NestedCheckBox
{
    /// <summary>
    /// A checkable tree node that behaves like a radio group: checking one child unchecks all of its
    /// siblings, instead of the default tri-state parent/child propagation of <see cref="CheckableItem"/>.
    /// </summary>
    public sealed class SingleCheckedCheckableItem : CheckableItem
    {
        /// <summary>Creates a leaf single-checked item.</summary>
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

        /// <summary>Creates a single-checked item with children, deriving its own checked state from them.</summary>
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

        /// <summary>Enforces the radio-button behavior: unchecks every other child, then checks the one that just changed.</summary>
        protected override void Child_OnCheckedChanged(object sender, EventArgs e)
        {
            Children.ForEach(c => c.SetChecked(false));
            var child = sender as CheckableItem;
            child.SetChecked(true);
        }
    }

    /// <summary>
    /// View model for one node of a nested checkbox tree (e.g. the file/context picker), supporting
    /// tri-state checking where a parent's state is derived from its children and toggling a parent
    /// propagates down to all of them.
    /// </summary>
    public class CheckableItem : BaseViewModel
    {
        /// <summary>Backing field for <see cref="IsChecked"/>.</summary>
        private bool? _isChecked;
        /// <summary>Visual style (colors, decoration, enabled state) applied to this item.</summary>
        private readonly CheckableItemStyle _style;

        /// <summary>Display name of this item.</summary>
        public string Name
        {
            get;
        }

        /// <summary>Longer description shown as a tooltip or secondary text for this item.</summary>
        public string Description
        {
            get;
        }

        /// <summary>Foreground brush for this item, switching between the style's checked and unchecked colors.</summary>
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

        /// <summary>Text decoration (e.g. strikethrough) applied to this item's label, from its style.</summary>
        public TextDecorationCollection TextDecoration => _style.TextDecoration;

        /// <summary>Whether the checkbox for this item can be interacted with.</summary>
        public bool IsEnabled => _style.IsEnabled;

        /// <summary>Caller-supplied value identifying what this item represents (e.g. a file or context entry).</summary>
        public object? Tag
        {
            get;
        }

        /// <summary>Child nodes nested under this item in the tree.</summary>
        public ObservableCollection<CheckableItem> Children
        {
            get;
        }

        /// <summary>Raised whenever this item's <see cref="IsChecked"/> state changes, so a parent can recompute its own state.</summary>
        public event EventHandler? OnCheckedChangedEvent;

        /// <summary>
        /// Tri-state checked flag: true/false when fully checked/unchecked, null when children are in a
        /// mixed state. Setting it propagates the new value down to every child.
        /// </summary>
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

        /// <summary>Creates a leaf item (no children) with an explicit initial checked state.</summary>
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

        /// <summary>Creates a parent item over an existing set of children, deriving its own checked state from theirs and subscribing to their change events.</summary>
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

        /// <summary>Appends a child item to this node, subscribes to its check-changed event, and recomputes this node's own checked state.</summary>
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

        /// <summary>Sets this item's checked state and propagates the same value down to every child, without re-raising <see cref="OnCheckedChangedEvent"/>.</summary>
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

        /// <summary>Default reaction to a child's checked state changing: recomputes this item's own tri-state value from all children.</summary>
        protected virtual void Child_OnCheckedChanged(object? sender, EventArgs e)
        {
            UpdateCheckedStatusFromChildren();
        }

        /// <summary>Recomputes and applies this item's checked state from its children's current states, firing the changed event if it differs.</summary>
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

        /// <summary>Derives a tri-state checked value from a set of children: true if all checked, false if none, null if mixed or empty of a definite state.</summary>
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

        /// <summary>Raises <see cref="OnCheckedChangedEvent"/> for this item.</summary>
        private void FireCheckedChanged()
        {
            OnCheckedChangedEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Immutable visual styling (colors, enabled state, text decoration) applied to a <see cref="CheckableItem"/>.</summary>
    public readonly struct CheckableItemStyle
    {
        /// <summary>A default enabled style with no explicit colors and no text decoration.</summary>
        public static readonly CheckableItemStyle Empty = new CheckableItemStyle(null, null, true, new TextDecorationCollection());

        /// <summary>Foreground used while the item is checked (or always, if <see cref="UncheckedForeground"/> is null).</summary>
        public readonly Brush? Foreground;
        /// <summary>Foreground used while the item is unchecked, if different from <see cref="Foreground"/>.</summary>
        public readonly Brush? UncheckedForeground;
        /// <summary>Whether the item's checkbox is interactable.</summary>
        public readonly bool IsEnabled;
        /// <summary>Text decoration (e.g. strikethrough) applied to the item's label.</summary>
        public readonly TextDecorationCollection TextDecoration;

        /// <summary>Creates a style with no text decoration.</summary>
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

        /// <summary>Creates a style with explicit colors, enabled state and text decoration.</summary>
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
