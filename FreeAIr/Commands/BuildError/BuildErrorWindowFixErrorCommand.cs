using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var lineEnding = LineEndingHelper.Actual.OpenDocumentAndGetLineEnding(
                errorInformation.FilePath
                );

            var wfd = new WholeFileTextDescriptor(
                errorInformation.FilePath,
                lineEnding
                );

            var contextItems = (await ContextComposer.ComposeFromFilePathAsync(
                errorInformation.FilePath
                )).ConvertToChatContextItem();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    ChatKindEnum.FixBuildError,
                    wfd
                    ),
                null
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
                    false
                    )
                );

            chat.ChatContext.AddItems(
                contextItems
                );

            var fi = new FileInfo(errorInformation.FilePath);

            chat.AddPrompt(
                UserPrompt.CreateFixBuildErrorPrompt(errorInformation.ErrorDescription, fi.Name, errorInformation.FilePath)
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }
    }
}
