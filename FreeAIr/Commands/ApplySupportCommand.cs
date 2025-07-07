using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Commands
{
    [Command(PackageIds.ApplySupportCommandId)]
    public sealed class ApplySupportCommand : BaseCommand<ApplySupportCommand>
    {
        public ApplySupportCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ApplySupportAction.Instance.ExecuteAsync();
        }

    }

    public sealed class ApplySupportAction : BaseApplySupportAction
    {
        public static readonly ApplySupportAction Instance = new();

        protected override async System.Threading.Tasks.Task<SupportActionJson> ChooseSupportAsync(
            )
        {
            var chosenSupport = await SupportContextMenu.ChooseSupportAsync(
                "Choose support:",
                SupportScopeEnum.SelectedCodeInDocument
                );
            return chosenSupport;
        }
    }


    public abstract class BaseApplySupportAction
    {
        public BaseApplySupportAction(
            )
        {
        }

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

            var chosenSupport = await ChooseSupportAsync();
            if (chosenSupport is null)
            {
                return;
            }

            var supportContext = await SupportContext.WithContextCodeAsync(
                std.FilePath
                );

            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                "Choose agent:",
                chosenSupport.AgentName
                );
            if (chosenAgent is null)
            {
                return;
            }

            var contextItems = (await CSharpContextComposer.ComposeFromActiveDocumentAsync(
                )).ConvertToChatContextItem();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    ChatKindEnum.Discussion,
                    std
                    ),
                null,
                await FreeAIr.BLogic.ChatOptions.GetDefaultAsync(chosenAgent)
                );
            if (chat is null)
            {
                return;
            }

            chat.ChatContext.AddItems(
                contextItems
                );

            var promptText = supportContext.ApplyVariablesToPrompt(
                chosenSupport.Prompt
                );

            chat.AddPrompt(
                UserPrompt.CreateTextBasedPrompt(
                    promptText
                    )
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }

        protected abstract System.Threading.Tasks.Task<SupportActionJson> ChooseSupportAsync(
            );
    }

}
