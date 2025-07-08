using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows;

namespace FreeAIr.BLogic
{
    public static class CommitMessageBuilder
    {
        public static async Task ChooseAgentAsync(
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                    "Choose support action:",
                    SupportScopeEnum.CommitMessageBuilding
                    );
                if (chosenSupportAction is null)
                {
                    return;
                }

                var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                    "Choose agent for commit message generation:",
                    chosenSupportAction.AgentName
                    );
                if (chosenAgent is null)
                {
                    return;
                }

                await BuildCommitMessageAsync(
                    chosenSupportAction,
                    chosenAgent
                    );
            }
            catch (Exception excp)
            {
                //todo log
            }
        }

        private static async Task BuildCommitMessageAsync(
            SupportActionJson action,
            AgentJson agent
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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

            var supportContext = await SupportContext.WithGitDiffAsync(
                gitDiff
                );

            var promptText = supportContext.ApplyVariablesToPrompt(
                action.Prompt
                );

            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    ChatKindEnum.GenerateCommitMessage,
                    null
                    ),
                UserPrompt.CreateTextBasedPrompt(promptText),
                await ChatOptions.NoToolAutoProcessedTextResponseAsync(agent)
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
}
