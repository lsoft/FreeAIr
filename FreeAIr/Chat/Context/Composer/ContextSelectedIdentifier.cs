using FreeAIr.UI.Embedillo.Answer.Parser;

namespace FreeAIr.Chat.Context.Composer
{
    /// <summary>
    /// A file the context composer has decided to offer, plus the one thing the composer knows
    /// about it that the file itself does not say: whether a human asked for it.
    ///
    /// Exists so the flag survives the trip from <see cref="CSharpContextComposer"/> to the chat
    /// context, where it decides how the item is shown and whether it may be dropped silently.
    /// </summary>
    public sealed class ContextSelectedIdentifier
    {
        /// <summary>The file, and the fragment of it, being offered.</summary>
        public SelectedIdentifier SelectedIdentifier
        {
            get;
        }

        /// <summary>
        /// True when the composer found this file by following references rather than being handed
        /// it by the user.
        /// </summary>
        public bool IsAutoFound
        {
            get;
        }

        /// <summary>Pairs a selected identifier with whether it was found automatically.</summary>
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
