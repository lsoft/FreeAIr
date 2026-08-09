namespace FreeAIr.Interaction
{
    /// <summary>
    /// The place where the aggregate state of all chats is shown to the user - in the shipping
    /// implementation, the little indicator FreeAIr injects into the Visual Studio status bar.
    /// The chat container reports into this and nothing more, which is all that kept it tied to
    /// the status bar control before.
    /// </summary>
    public interface IChatStatusIndicator
    {
        /// <summary>
        /// Reports that the chats as a whole are now working or idle. Safe to call from any
        /// thread; the implementation switches to the UI thread itself.
        /// </summary>
        void UpdateStatus(ChatsStatusEnum status);
    }

    /// <summary>
    /// The chat activity state shown by the status indicator.
    /// </summary>
    public enum ChatsStatusEnum
    {
        /// <summary>
        /// At least one chat has a request in progress.
        /// </summary>
        Working,

        /// <summary>
        /// No chat currently has a request in progress.
        /// </summary>
        Idle
    }
}
