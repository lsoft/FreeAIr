using FreeAIr.Agents;
using FreeAIr.Git;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows;

namespace FreeAIr.BLogic
{
    public static class CommitMessageBuilder
    {
        public static async Task BuildCommitMessageAsync(
            Agent agent
            )
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var gitWindowModifier = componentModel.GetService<GitWindowModifier>();

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

            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    ChatKindEnum.GenerateCommitMessage,
                    null
                    ),
                UserPrompt.CreateCommitMessagePrompt(gitDiff),
                ChatOptions.NoToolAutoProcessedTextResponse(agent)
                );

            if (chat is not null)
            {
                var commitMessage = await chat.WaitForPromptCleanAnswerAsync(
                    Environment.NewLine
                    );
                if (!string.IsNullOrEmpty(commitMessage))
                {
                    gitWindowModifier.CommitMessageTextBox.Text = commitMessage;
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

    public sealed class CommitMessageBuilderCommandProcessor : GitDiffItemsCommandProcessor
    {
        public static new readonly CommitMessageBuilderCommandProcessor Instance = new();

        public override async Task ProcessAsync(Agent agent)
        {
            var contextItems = await CreateContextItemsAsync();
            if (contextItems is null || contextItems.Count == 0)
            {
                return;
            }

            await CommitMessageBuilder.BuildCommitMessageAsync(
                agent
                );
        }
    }

}
