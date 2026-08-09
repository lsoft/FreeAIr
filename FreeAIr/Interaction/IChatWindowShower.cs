namespace FreeAIr.Interaction
{
    /// <summary>
    /// Brings a chat in front of the user, wherever chats are shown - the in-situ popup at the
    /// editor caret or the docked chat list, as the UI options page dictates. Callers that merely
    /// want the user to see a chat they started go through this instead of knowing about either
    /// window.
    /// </summary>
    public interface IChatWindowShower
    {
        /// <summary>
        /// Shows the given chat, letting the options page choose the presentation unless one of
        /// the force flags demands the in-situ popup or the docked tool window.
        /// </summary>
        Task ShowChatWindowAsync(
            Chat.Chat chat,
            bool inSituForce = false,
            bool toolForce = false
            );
    }
}
