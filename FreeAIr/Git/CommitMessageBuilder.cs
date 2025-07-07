using FreeAIr.BLogic.Context.Item;
using FreeAIr.Find;
using FreeAIr.Git;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Windows;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
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

                var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();
                var chosenAgent = await VisualStudioContextMenuCommandBridge.ShowAsync<AgentJson>(
                    "Choose agent for commit message generation:",
                    agentCollection.Agents.ConvertAll(a => (a.Name, (object)a))
                    );
                if (chosenAgent is null)
                {
                    return;
                }

                await BuildCommitMessageAsync(
                    chosenAgent
                    );
            }
            catch (Exception excp)
            {
                //todo log
            }
        }

        private static async Task BuildCommitMessageAsync(
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

            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    ChatKindEnum.GenerateCommitMessage,
                    null
                    ),
                await UserPrompt.CreateCommitMessagePromptAsync(gitDiff),
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
