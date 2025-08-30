namespace FreeAIr.Chat.Context
{
    public sealed class ChatContextEventArgs : EventArgs
    {
        public ChatContext Context
        {
            get;
        }

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
