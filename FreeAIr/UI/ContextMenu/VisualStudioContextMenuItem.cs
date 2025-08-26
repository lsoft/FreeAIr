namespace FreeAIr.UI.ContextMenu
{
    public sealed class VisualStudioContextMenuItem
    {
        public string Title
        {
            get;
        }

        public bool IsChecked
        {
            get;
        }

        public bool IsEnabled
        {
            get;
        }

        public object? Tag
        {
            get;
        }

        public VisualStudioContextMenuItem(
            string title
            )
        {
            Title = title;
            IsChecked = false;
            IsEnabled = false;
            Tag = null;
        }

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
