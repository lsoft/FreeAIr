using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.IO;
using System.Linq;

namespace FreeAIr.Commands
{
    [Command(PackageIds.ExplainFileCommandId)]
    internal sealed class ExplainFileCommand : BaseCommand<ExplainFileCommand>
    {
        public ExplainFileCommand(
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

            var code = File.ReadAllText(selectedFile.FullPath);
            if (string.IsNullOrEmpty(code))
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    "File is empty"
                    );
                return;
            }

            var fileName = new FileInfo(selectedFile.FullPath).Name;

            var kind = ChatKindEnum.ExplainCode;

            chatContainer.StartChat(
                new ChatDescription(
                    kind,
                    null
                    ),
                UserPrompt.CreateCodeBasedPrompt(kind, fileName, code)
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }

    }
}
