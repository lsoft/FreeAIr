using EnvDTE;
using FreeAIr.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Item;
using System.Windows.Input;

namespace FreeAIr.Commands
{
    [Command(PackageIds.StartDiscussionCommandId)]
    internal sealed class StartDiscussionCommand : BaseCommand<StartDiscussionCommand>
    {
        public StartDiscussionCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var std = await TextDescriptorHelper.GetSelectedTextAsync();
            if (std is null || std.SelectedSpan is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoSelectedCode
                    );
                return;
            }

            var chat = chatContainer.GetLastUsed();

            if (chat == null)
            {
                var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                    "Choose agent:"
                    );
                if (chosenAgent is null)
                {
                    return;
                }


                    chat = await chatContainer.StartChatAsync(
                    new ChatDescription(
                        null
                        ),
                    null,
                    await FreeAIr.Chat.ChatOptions.GetDefaultAsync(chosenAgent)
                    );
                chatContainer.LastUsedChatId = chat.Id;
            }
            if (chat == null) return;

            try
            {
                chat.ChatContext.AddItem(
                    new SolutionItemChatContextItem(
                        std.CreateSelectedIdentifier(),
                        false,
                        AddLineNumbersMode.NotRequired
                        )
                    );
            }
            catch (Exception ex)
            {
                int a = 1;
            }

            await ChatWindowShower.ShowChatWindowAsync(chat);
        }

    }
}
