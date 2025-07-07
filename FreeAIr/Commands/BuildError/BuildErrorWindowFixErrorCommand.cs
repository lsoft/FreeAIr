using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.IO;

namespace FreeAIr.Commands.BuildError
{
    [Command(PackageIds.BuildErrorWindowFixErrorCommandId)]
    public sealed class BuildErrorWindowFixErrorCommand : BaseCommand<BuildErrorWindowFixErrorCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var errorInformation = await FreeAIr.BuildErrors.BuildResultProvider.GetSelectedErrorInformationAsync();
            if (errorInformation is null)
            {
                return;
            }

            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                "Choose agent:"
                );
            if (chosenAgent is null)
            {
                return;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var lineEnding = LineEndingHelper.Actual.OpenDocumentAndGetLineEnding(
                errorInformation.FilePath
                );

            var wfd = new WholeFileTextDescriptor(
                errorInformation.FilePath,
                lineEnding
                );

            var contextItems = (await CSharpContextComposer.ComposeFromFilePathAsync(
                errorInformation.FilePath
                )).ConvertToChatContextItem();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    ChatKindEnum.FixBuildError,
                    wfd
                    ),
                null,
                await FreeAIr.BLogic.ChatOptions.GetDefaultAsync(chosenAgent)
                );
            if (chat is null)
            {
                return;
            }

            chat.ChatContext.AddItem(
                new SolutionItemChatContextItem(
                    new UI.Embedillo.Answer.Parser.SelectedIdentifier(
                        errorInformation.FilePath,
                        null
                        ),
                    false,
                    AddLineNumbersMode.NotRequired
                    )
                );

            chat.ChatContext.AddItems(
                contextItems
                );

            var fi = new FileInfo(errorInformation.FilePath);

            chat.AddPrompt(
                await UserPrompt.CreateFixBuildErrorPromptAsync(errorInformation.ErrorDescription, fi.Name, errorInformation.FilePath)
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }
    }
}
