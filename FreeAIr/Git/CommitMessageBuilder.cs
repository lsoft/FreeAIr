using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.BLogic
{
    public static class CommitMessageBuilder
    {
        public static async Task BuildCommitMessageAsync(
            TextBox commitMessageTextBox
            )
        {
            if (commitMessageTextBox is null)
            {
                throw new ArgumentNullException(nameof(commitMessageTextBox));
            }

            var backgroundTask = new GitCollectBackgroundTask(
                    );
            var w = new WaitForTaskWindow(
                backgroundTask
                );
            await w.ShowDialogAsync();

            var gitDiff = backgroundTask.Result;
            if (string.IsNullOrEmpty(gitDiff))
            {
                await ShowErrorAsync("Cannot collect git patch. Please enter commit message manually.");
                return;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    ChatKindEnum.GenerateCommitMessage,
                    null
                    ),
                UserPrompt.CreateCommitMessagePrompt(gitDiff),
                ChatOptions.NoToolAutoProcessedTextResponse
                );

            if (chat is not null)
            {
                var commitMessage = await chat.WaitForPromptCleanAnswerAsync(
                    Environment.NewLine
                    );
                if (!string.IsNullOrEmpty(commitMessage))
                {
                    commitMessageTextBox.Text = commitMessage;
                    return;
                }
            }

            ShowErrorAsync("Cannot receive AI answer. Please enter commit message manually.")
                .FileAndForget(nameof(ShowErrorAsync));

            await ChatListToolWindow.ShowIfEnabledAsync();
        }

        private static async Task ShowErrorAsync(
            string error
            )
        {
            await VS.MessageBox.ShowErrorAsync(
                Resources.Resources.Error,
                error
                );
        }
    }
}
