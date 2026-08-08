using EnvDTE;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Item;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Diagnostics;

namespace FreeAIr.Commands
{
    /// <summary>
    /// The editor context-menu command that starts (or reuses) a chat and adds the currently
    /// selected code as a context item, so the user can immediately discuss that selection.
    /// </summary>
    [Command(PackageIds.StartDiscussionCommandId)]
    public sealed class StartDiscussionCommand : CreateOrReuseChatCommand<StartDiscussionCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public StartDiscussionCommand(
            )
        {
        }

        /// <summary>
        /// Validates there is a text selection, creates or reuses a chat, adds the selection as a
        /// context item, and shows the chat window.
        /// </summary>
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
