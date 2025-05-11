using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.BLogic.Context.Composer;
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

namespace FreeAIr.Commands.BuildError
{
    [Command(PackageIds.BuildErrorWindowFixErrorCommandId)]
    public sealed class BuildErrorWindowFixErrorCommand : BaseCommand<BuildErrorWindowFixErrorCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var errorInformation = await GetErrorInformationAsync();
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

        private async Task<ErrorInformation?> GetErrorInformationAsync()
        {
            if (await FreeAIrPackage.Instance.GetServiceAsync(typeof(SVsErrorList)) is not IVsTaskList2 tasks)
            {
                return null;
            }

            tasks.EnumSelectedItems(out IVsEnumTaskItems itemsEnum);

            var vsTaskItem = new IVsTaskItem[1];

            if (itemsEnum.Next(1, vsTaskItem, null) == 0)
            {
                var taskItem = vsTaskItem[0];
                var errorTaskItem = taskItem as IVsErrorItem;
                if (errorTaskItem is null)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                var categoryResult = errorTaskItem.GetCategory(out uint category);
                if (categoryResult != VSConstants.S_OK)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }
                if (category.NotIn((uint)TaskErrorCategory.Error))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "FreeAIr is supposed to fix only errors. If you want analyze build warning, file an issue to its dev."
                        );
                    return null;
                }

                var documentResult = taskItem.Document(out string document);
                if (documentResult != VSConstants.S_OK || string.IsNullOrEmpty(document))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                var lineResult = taskItem.Line(out int line);
                if (lineResult != VSConstants.S_OK)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                var columnResult = taskItem.Column(out int column);
                if (columnResult != VSConstants.S_OK)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                var getTextResult = taskItem.get_Text(out string description);
                if (getTextResult != VSConstants.S_OK || string.IsNullOrEmpty(description))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                return new ErrorInformation(
                    document,
                    description,
                    line,
                    column
                    );

            }

            return null;
        }
    }
}
