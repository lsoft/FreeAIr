using EnvDTE;
using FreeAIr.Agents;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Embedding;
using FreeAIr.Git;
using FreeAIr.Helper;
using FreeAIr.NLOutline;
using FreeAIr.NLOutline.Tree.Builder;
using FreeAIr.UI;
using FreeAIr.UI.ContextMenu;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace FreeAIr.Commands.File
{
    [Command(PackageIds.BuildNaturalLanguageOutlinesTreeCommandId)]
    public sealed class BuildNaturalLanguageOutlinesTreeCommand : BaseCommand<BuildNaturalLanguageOutlinesTreeCommand>
    {
        public BuildNaturalLanguageOutlinesTreeCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await BuildNaturalLanguageOutlinesJsonFileToolWindow.ShowPaneAsync(
                true
                );
        }
    }
}
