using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
using static FreeAIr.UI.ViewModels.SolutionItemsContextMenuCommandBridge;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.SolutionItemsContextMenuDynamicCommandId)]
    public sealed class SolutionItemsContextMenuCommand : BaseDynamicCommand<SolutionItemsContextMenuCommand, SolutionItemContextMenuItem>
    {
        private SolutionItemsContextMenuCommandBridge _bridge;

        protected override async Task InitializeCompletedAsync()
        {
            _bridge = await FreeAIrPackage.Instance.GetServiceAsync<SolutionItemsContextMenuCommandBridge, SolutionItemsContextMenuCommandBridge>();
        }

        protected override IReadOnlyList<SolutionItemContextMenuItem> GetItems()
        {
            return _bridge.MenuItems;
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuItem, EventArgs e, SolutionItemContextMenuItem item)
        {
            menuItem.Text = item.Title;
            menuItem.Checked = false;
        }

        protected override void Execute(OleMenuCmdEventArgs e, SolutionItemContextMenuItem item)
        {
            item.InvokeCommand();
        }
    }
}
