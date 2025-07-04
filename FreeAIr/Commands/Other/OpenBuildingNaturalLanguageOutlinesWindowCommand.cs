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

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.OpenBuildingNaturalLanguageOutlinesWindowCommandId)]
    public sealed class OpenBuildingNaturalLanguageOutlinesWindowCommand : BaseCommand<OpenBuildingNaturalLanguageOutlinesWindowCommand>
    {
        public OpenBuildingNaturalLanguageOutlinesWindowCommand(
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
