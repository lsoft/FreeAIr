using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.IO;
using System.Linq;

namespace FreeAIr.Commands
{
    [Command(PackageIds.AddCommentsToFileCommandId)]
    internal sealed class AddCommentsToFileCommand : BaseCommand<AddCommentsToFileCommand>
    {
        public AddCommentsToFileCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (string.IsNullOrEmpty(ApiPage.Instance.Token))
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoToken
                    );
                return;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = (await sew.GetSelectionAsync()).ToList();
            if (selections.Count != 1)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    "Please select only one item."
                    );
                return;
            }

            var selectedFile = selections[0];
            if (selectedFile.Type != SolutionItemType.PhysicalFile)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    "Selected item is not a physical file."
                    );
                return;
            }

            var kind = ChatKindEnum.AddXmlComments;

            var lineEnding = LineEndingHelper.Actual.OpenDocumentAndGetLineEnding(
                selectedFile.FullPath
                );

            var wfd = new WholeFileTextDescriptor(
                selectedFile.FullPath,
                lineEnding
                );

            chatContainer.StartChat(
                new ChatDescription(
                    kind,
                    wfd
                    ),
                UserPrompt.CreateCodeBasedPrompt(kind, wfd.FileName, wfd.OriginalText)
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }
    }

}
