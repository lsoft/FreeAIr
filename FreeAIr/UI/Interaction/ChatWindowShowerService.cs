using FreeAIr.Interaction;
using FreeAIr.UI.ToolWindows;
using System.ComponentModel.Composition;

namespace FreeAIr.UI.Interaction
{
    /// <summary>
    /// Answers <see cref="IChatWindowShower"/> with the static <see cref="ChatWindowShower"/> the
    /// commands in this assembly already use, so callers outside it need no knowledge of either
    /// chat window.
    /// </summary>
    [Export(typeof(IChatWindowShower))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ChatWindowShowerService : IChatWindowShower
    {
        /// <inheritdoc/>
        public async Task ShowChatWindowAsync(
            FreeAIr.Chat.Chat chat,
            bool inSituForce = false,
            bool toolForce = false
            )
        {
            await ChatWindowShower.ShowChatWindowAsync(
                chat,
                inSituForce,
                toolForce
                );
        }
    }
}
