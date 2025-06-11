using FreeAIr.Commands.ContextMenu;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
using static FreeAIr.UI.ViewModels.SolutionItemsContextMenuCommandBridge;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.SolutionItemsContextMenuDynamicCommandId)]
    public sealed class SolutionItemsContextMenuCommand
        : Base_ContextMenu_DynamicCommand<SolutionItemsContextMenuCommand, SolutionItemContextMenuItem, SolutionItemsContextMenuCommandBridge>
    {
        protected override Task ExecuteAsync(SolutionItemContextMenuItem item)
        {
            item.InvokeCommand();

            return Task.CompletedTask;
        }
    }
}
