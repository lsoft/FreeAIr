namespace FreeAIr.Chat.Context
{
    /// <summary>
    /// Event payload for <see cref="ChatContext.ChatContextChangedEvent"/>, carrying the
    /// <see cref="ChatContext"/> whose items just changed so subscribers can redraw the context
    /// chips under the prompt box.
    /// </summary>
    public sealed class ChatContextEventArgs : EventArgs
    {
        /// <summary>The context that changed.</summary>
        public ChatContext Context
        {
            get;
        }

        /// <summary>Wraps the given context, rejecting a null one.</summary>
        public ChatContextEventArgs(
            ChatContext context
            )
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Context = context;
        }

    }

}
