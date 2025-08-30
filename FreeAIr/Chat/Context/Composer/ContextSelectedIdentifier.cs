using FreeAIr.UI.Embedillo.Answer.Parser;

namespace FreeAIr.Chat.Context.Composer
{
    public sealed class ContextSelectedIdentifier
    {
        public SelectedIdentifier SelectedIdentifier
        {
            get;
        }
        public bool IsAutoFound
        {
            get;
        }

        public ContextSelectedIdentifier(
            SelectedIdentifier selectedIdentifier,
            bool isAutoFound
            )
        {
            if (selectedIdentifier is null)
            {
                throw new ArgumentNullException(nameof(selectedIdentifier));
            }

            SelectedIdentifier = selectedIdentifier;
            IsAutoFound = isAutoFound;
        }
    }
}
