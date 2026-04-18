using WpfHelpers;

namespace FreeAIr.Chat
{
    public sealed class ChatDescription : BaseViewModel, IDisposable
    {
        public string Title
        {
            get;
            set;
        }

        public IOriginalTextDescriptor? SelectedTextDescriptor
        {
            get;
        }

        public ChatDescription(
            IOriginalTextDescriptor? selectedTextDescriptor
            )
        {
            SelectedTextDescriptor = selectedTextDescriptor;
            Title = "Untitled";
        }

        protected override void DisposeViewModel()
        {
            SelectedTextDescriptor?.Dispose();
        }
    }
}
