using FreeAIr.Helper;
using System.Collections.Generic;

namespace FreeAIr.UI.ContextMenu
{
    [Command(PackageIds.VisualStudioContextMenuDynamicCommandId)]
    public sealed class VisualStudioContextMenu_DynamicCommand
        : BaseDynamicCommand<VisualStudioContextMenu_DynamicCommand, VisualStudioContextMenuItem>
    {
        private VisualStudioContextMenuCommandBridge _bridge;

        protected override async Task InitializeCompletedAsync()
        {
            _bridge = await FreeAIrPackage.Instance.GetServiceAsync<VisualStudioContextMenuCommandBridge, VisualStudioContextMenuCommandBridge>();
        }

        protected override IReadOnlyList<VisualStudioContextMenuItem> GetItems()
        {
            return _bridge.MenuItems;
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuItem, EventArgs e, VisualStudioContextMenuItem item)
        {
            menuItem.Text = item.Title;
            menuItem.Checked = item.IsChecked;
            menuItem.Enabled = item.IsEnabled;
        }

        protected override void Execute(OleMenuCmdEventArgs e, VisualStudioContextMenuItem menuItem)
        {
            try
            {
                _bridge.ChosenItem = menuItem;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }
    }


}
