namespace FreeAIr.UI.ContextMenu
{
    public sealed class VisualStudioContextMenuItem
    {
        public string Title
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
            IsEnabled = false;
            Tag = null;
        }

        public VisualStudioContextMenuItem(
            string title,
            object tag
            )
        {
            Title = title;
            IsEnabled = true;
            Tag = tag;
        }
    }


}
