namespace FreeAIr.Chat
{
    public sealed class ChatDescription : IDisposable
    {
        public IOriginalTextDescriptor? SelectedTextDescriptor
        {
            get;
        }

        public ChatDescription(
            IOriginalTextDescriptor? selectedTextDescriptor
            )
        {
            SelectedTextDescriptor = selectedTextDescriptor;
        }

        public void Dispose()
        {
            SelectedTextDescriptor?.Dispose();
        }
    }
}
