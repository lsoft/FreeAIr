using EnvDTE;
using EnvDTE80;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Commands
{
    [Command(PackageIds.FreeAIrNoteCommandId)]
    internal sealed class AddCommentsCommand : BaseCommand<AddCommentsCommand>
    {
        public AddCommentsCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var taskContainer = componentModel.GetService<ChatContainer>();

            var (fileName, selectedCode) = await DocumentHelper.GetSelectedTextAsync();
            if (fileName is null || string.IsNullOrEmpty(selectedCode))
            {
                await VS.MessageBox.ShowWarningAsync(
                    "Error",
                    "Cannot obtain selected block of code"
                    );
                return;
            }

            var kind = ChatKindEnum.AddComments;

            taskContainer.StartChat(
                new ChatDescription(
                    kind,
                    fileName
                    ),
                UserPrompt.CreateCodeBasedPrompt(kind, selectedCode)
                );

            if (ResponsePage.Instance.SwitchToTaskWindow)
            {
                _ = await ChatListToolWindow.ShowAsync();
            }
        }

    }
}
