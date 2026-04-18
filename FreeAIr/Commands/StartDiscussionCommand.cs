using EnvDTE;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Item;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Diagnostics;

namespace FreeAIr.Commands
{
    [Command(PackageIds.StartDiscussionCommandId)]
    public sealed class StartDiscussionCommand : CreateOrReuseChatCommand<StartDiscussionCommand>
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

            var chat = await CreateOrReuseChatAsync();
            if (chat is null)
            {
                return;
            }

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
                Debug.WriteLine( ex.Message );
            }

            await ChatWindowShower.ShowChatWindowAsync(chat);
        }
    }

}
