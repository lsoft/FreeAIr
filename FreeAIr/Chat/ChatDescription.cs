using WpfHelpers;

namespace FreeAIr.Chat
{
    /// <summary>
    /// What a chat looks like in the chat list: its title, and the piece of code it was started
    /// from.
    ///
    /// Kept apart from the chat itself because the list window binds to this and nothing else — a
    /// chat carries its prompts, its answers and a live connection, none of which the list needs to
    /// draw a row.
    /// </summary>
    public sealed class ChatDescription : BaseViewModel, IDisposable
    {
        /// <summary>
        /// The row caption. Starts as `Untitled` and is replaced later, usually with a summary of
        /// the first prompt, so a list of chats can be told apart at a glance.
        /// </summary>
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// The editor selection this chat was opened on, or null for a chat started from scratch.
        /// It keeps pointing at the original span while the file is edited, which is what lets an
        /// answer be applied back to the right place long after the request was made.
        /// </summary>
        public IOriginalTextDescriptor? SelectedTextDescriptor
        {
            get;
        }

        /// <summary>
        /// Creates the description for a new chat, optionally anchored to a selection in the editor.
        /// </summary>
        public ChatDescription(
            IOriginalTextDescriptor? selectedTextDescriptor
            )
        {
            SelectedTextDescriptor = selectedTextDescriptor;
            Title = "Untitled";
        }

        /// <summary>Releases the tracking span held by <see cref="SelectedTextDescriptor"/>, if any.</summary>
        protected override void DisposeViewModel()
        {
            //the descriptor holds a tracking span in the editor buffer, which keeps the document
            //alive until it is let go
            SelectedTextDescriptor?.Dispose();
        }
    }
}
