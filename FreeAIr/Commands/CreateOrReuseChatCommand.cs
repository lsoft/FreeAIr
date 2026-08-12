using FreeAIr.Chat;
using FreeAIr.Helper;
using FreeAIr.UI.ContextMenu;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FreeAIr.Commands
{
    /// <summary>
    /// Base for menu commands that either reuse the most recently used chat (when the user holds
    /// Ctrl while invoking the command) or start a new one after prompting for an agent to use.
    /// </summary>
    public abstract class CreateOrReuseChatCommand<T> : BaseCommand<T>
        where T: BaseCommand<T>, new()
    {
        /// <summary>
        /// Returns the last-used chat if Ctrl is held down when the command runs; otherwise lets
        /// the user pick an agent and starts a fresh chat with it.
        /// </summary>
        protected async Task<Chat.Chat?> CreateOrReuseChatAsync(
            )
        {
            var componentModel = await MefHelper.GetComponentModelAsync();
            var chatContainer = componentModel.GetService<ChatContainer>();

            var ctrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            if (ctrlPressed)
            {
                var lastUsedChat = chatContainer.GetLastCreatedChat();
                if (lastUsedChat is not null)
                {
                    return lastUsedChat;
                }
            }

            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                FreeAIr.Resources.Resources.Choose_agent__with_a_non_empty_token
                );
            if (chosenAgent is null)
            {
                return null;
            }

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(null),
                null,
                await FreeAIr.Chat.ChatOptions.GetDefaultAsync(chosenAgent)
                );

            return chat;
        }
    }

}
