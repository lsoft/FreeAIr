using FreeAIr.Shared.ish;

namespace FreeAIr.Chat
{
    public sealed class ChatDescription : cNotifyBase, IDisposable
    {

        public string Title
        {
            get => field;
            set => SetProperty(ref field, value);
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

        public void Dispose()
        {
            SelectedTextDescriptor?.Dispose();
        }
    }
}
