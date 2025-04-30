using EnvDTE;
using EnvDTE80;
using FreeAIr.BLogic.Tasks;
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
    [Command(PackageIds.FreeAIrExplainCommandId)]
    internal sealed class ExplainCommand : BaseCommand<ExplainCommand>
    {
        public ExplainCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var taskContainer = componentModel.GetService<AITaskContainer>();

            var (fileName, selectedText) = await DocumentHelper.GetSelectedTextAsync();
            if (fileName is null || selectedText is null)
            {
                return;
            }

            taskContainer.StartTask(
                new TaskKind(
                    AITaskKindEnum.ExplainCode,
                    fileName
                    ),
                selectedText
                );

            _ = await TaskListToolWindow.ShowAsync();
        }

    }
}
