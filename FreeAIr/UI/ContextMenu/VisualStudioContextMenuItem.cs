namespace FreeAIr.UI.ContextMenu
{
    /// <summary>
    /// Describes one entry of FreeAIr's dynamic Visual Studio context menu (e.g. a support action or
    /// scope choice), rendered by <see cref="VisualStudioContextMenu_DynamicCommand"/>.
    /// </summary>
    public sealed class VisualStudioContextMenuItem
    {
        /// <summary>Text shown for this menu entry.</summary>
        public string Title
        {
            get;
        }

        /// <summary>Whether this entry is rendered with a checkmark.</summary>
        public bool IsChecked
        {
            get;
        }

        /// <summary>Whether this entry can be clicked.</summary>
        public bool IsEnabled
        {
            get;
        }

        /// <summary>Caller-supplied value identifying what this entry represents, returned via <c>ChosenItem</c> when it is picked.</summary>
        public object? Tag
        {
            get;
        }

        /// <summary>Creates a disabled, unchecked placeholder entry with only a title (used e.g. for a "no items" row).</summary>
        public VisualStudioContextMenuItem(
            string title
            )
        {
            Title = title;
            IsChecked = false;
            IsEnabled = false;
            Tag = null;
        }

        /// <summary>Creates an enabled, clickable menu entry carrying the tag returned when the user picks it.</summary>
        public VisualStudioContextMenuItem(
            string title,
            bool isChecked,
            object tag
            )
        {
            Title = title;
            IsChecked = isChecked;
            IsEnabled = true;
            Tag = tag;
        }
    }


}
