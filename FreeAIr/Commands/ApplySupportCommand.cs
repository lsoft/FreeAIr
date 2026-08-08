using EnvDTE;
using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Composer;

namespace FreeAIr.Commands
{
    /// <summary>
    /// The menu command that lets the user pick a support action (from Options2) for the code
    /// currently selected in the editor and starts a chat that applies it.
    /// </summary>
    [Command(PackageIds.ApplySupportCommandId)]
    public sealed class ApplySupportCommand : BaseCommand<ApplySupportCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public ApplySupportCommand(
            )
        {
        }

        /// <summary>
        /// Runs when the user invokes the command; delegates to the shared <see cref="ApplySupportAction"/> singleton.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ApplySupportAction.Instance.ExecuteAsync();
        }

    }

    /// <summary>
    /// Concrete support action for the "apply support to selected code" command; prompts the user
    /// to choose a support action scoped to the code currently selected in the document.
    /// </summary>
    public sealed class ApplySupportAction : BaseApplySupportAction
    {
        /// <summary>
        /// The single shared instance of this action, reused by <see cref="ApplySupportCommand"/>.
        /// </summary>
        public static readonly ApplySupportAction Instance = new();

        /// <summary>
        /// Shows the support action picker scoped to the currently selected code in the document.
        /// </summary>
        protected override async System.Threading.Tasks.Task<SupportActionJson> ChooseSupportAsync(
            )
        {
            var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                "Choose support action:",
                SupportScopeEnum.SelectedCodeInDocument
                );
            return chosenSupportAction;
        }
    }


    /// <summary>
    /// Shared workflow behind the "apply support" commands: takes the currently selected code,
    /// lets the user choose a support action (from Options2/Support) and an agent, then starts a
    /// chat pre-loaded with the composed context and the support action's prompt.
    /// </summary>
    public abstract class BaseApplySupportAction
    {
        /// <summary>
        /// Creates the base action; derived classes provide the actual support-action picker.
        /// </summary>
        public BaseApplySupportAction(
            )
        {
        }

        /// <summary>
        /// Runs the full apply-support flow: validates there is a selection, lets the user choose
        /// the support action and agent, composes the C# context, and opens a chat with the
        /// resulting prompt.
        /// </summary>
        public async Task ExecuteAsync()
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

            var chosenSupportAction = await ChooseSupportAsync();
            if (chosenSupportAction is null)
            {
                return;
            }

            var supportContext = await SupportContext.WithContextItemAsync(
                std.FilePath
                );

            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                "Choose agent:",
                chosenSupportAction.AgentName
                );
            if (chosenAgent is null)
            {
                return;
            }

            var contextItems = (await CSharpContextComposer.ComposeFromActiveDocumentAsync(
                )).ConvertToChatContextItem();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    std
                    ),
                null,
                await FreeAIr.Chat.ChatOptions.GetDefaultAsync(chosenAgent)
                );
            if (chat is null)
            {
                return;
            }

            chat.ChatContext.AddItems(
                contextItems
                );

            var promptText = supportContext.ApplyVariablesToPrompt(
                chosenSupportAction.Prompt
                );

            chat.AddPrompt(
                UserPrompt.CreateTextBasedPrompt(
                    promptText
                    )
                );

            await ChatWindowShower.ShowChatWindowAsync(chat);
        }

        /// <summary>
        /// Lets the user pick which support action (from Options2/Support) to apply; scope differs
        /// between derived implementations (e.g. selected code vs. build error window).
        /// </summary>
        protected abstract System.Threading.Tasks.Task<SupportActionJson> ChooseSupportAsync(
            );
    }

}
