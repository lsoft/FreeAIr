using FreeAIr.Helper;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows;
using FreeAIr.Chat;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// Generates a commit message from the current git diff and writes it into the Team Explorer /
    /// Git Changes commit message box. Backs the "Build commit message with AI" command.
    /// </summary>
    public static class CommitMessageBuilder
    {
        /// <summary>
        /// Asks the user which support action and agent to use, then builds the commit message with
        /// them.
        /// </summary>
        public static async Task ChooseAgentAsync(
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                    Resources.Resources.Choose_support_action,
                    SupportScopeEnum.CommitMessageBuilding
                    );
                if (chosenSupportAction is null)
                {
                    return;
                }

                var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                    FreeAIr.Resources.Resources.Choose_agent_for_commit_message_generation,
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
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Collects the pending git diff in a wait dialog, sends it to the chosen agent through the
        /// given support action, and writes the answer into Visual Studio's commit message box.
        /// Shows an error and opens the chat window when the diff cannot be collected or the agent
        /// gives no answer.
        /// </summary>
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
                await ShowErrorAsync(FreeAIr.Resources.Resources.Cannot_collect_git_patch__Please);
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
            ShowErrorAsync(FreeAIr.Resources.Resources.Cannot_receive_AI_answer__Please)
                .FileAndForget(nameof(ShowErrorAsync));

            await ChatWindowShower.ShowChatWindowAsync(chat);
        }

        /// <summary>
        /// Shows a modal error message box with the given text.
        /// </summary>
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
