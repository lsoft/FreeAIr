using FreeAIr.Chat.Content;

namespace FreeAIr.UI.Dialog
{
    /// <summary>
    /// The chat's timeline as one bubble of it is allowed to act on the whole: cut it back to that
    /// bubble, or branch a new chat off it.
    ///
    /// A bubble is built from a single transcript entry and knows nothing about the chat which
    /// holds it, while <see cref="FreeAIr.UI.ViewModels.DialogViewModel"/> knows both — so the view
    /// model is what implements this, and the bubble only asks for what it cannot reach itself.
    /// </summary>
    public interface IChatTimeline
    {
        /// <summary>
        /// Whether there is anything to cut after <paramref name="content"/> and the chat is idle
        /// enough to allow it. Read by the Rewind command itself, so the link greys out on its own
        /// while the model is answering, and under the newest answer, which nothing follows.
        /// </summary>
        bool CanRewindAfter(IChatContent content);

        /// <summary>
        /// Asks the user to confirm, and then drops everything said after <paramref name="content"/>
        /// — from the chat window, from the chat, and from the chat's file on disk.
        /// </summary>
        Task RewindAfterAsync(IChatContent content);

        /// <summary>
        /// Whether a fork may be started at <paramref name="content"/> right now. Unlike a rewind
        /// this is allowed at the newest entry too — a fork of the whole chat is a copy of it — and
        /// it is refused only while the transcript is being written into.
        /// </summary>
        bool CanForkAt(IChatContent content);

        /// <summary>
        /// Starts a new chat holding everything up to and including <paramref name="content"/>, and
        /// switches to it. The chat being read is left exactly as it was, so this asks nothing.
        /// </summary>
        Task ForkAtAsync(IChatContent content);
    }
}
