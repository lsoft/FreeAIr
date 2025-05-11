namespace FreeAIr.BLogic
{
    public sealed class ChatDescription : IDisposable
    {
        public ChatKindEnum Kind
        {
            get;
        }
        public IOriginalTextDescriptor? SelectedTextDescriptor
        {
            get;
        }

        public ChatDescription(
            ChatKindEnum kind,
            IOriginalTextDescriptor? selectedTextDescriptor
            )
        {
            Kind = kind;
            SelectedTextDescriptor = selectedTextDescriptor;
        }

        public void Dispose()
        {
            SelectedTextDescriptor?.Dispose();
        }
    }
}
