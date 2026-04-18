using FreeAIr.Chat;
using FreeAIr.UI.ContextMenu;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FreeAIr.Commands
{
    public abstract class CreateOrReuseChatCommand<T> : BaseCommand<T>
        where T: BaseCommand<T>, new()
    {
        protected async Task<Chat.Chat?> CreateOrReuseChatAsync(
            )
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
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
