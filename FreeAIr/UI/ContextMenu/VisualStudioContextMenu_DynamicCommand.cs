using FreeAIr.Helper;
using System.Collections.Generic;

namespace FreeAIr.UI.ContextMenu
{
    /// <summary>
    /// Dynamic Visual Studio command that renders FreeAIr's context menu items (populated at runtime by
    /// <see cref="VisualStudioContextMenuCommandBridge"/>) and reports back which entry the user picked.
    /// </summary>
    [Command(PackageIds.VisualStudioContextMenuDynamicCommandId)]
    public sealed class VisualStudioContextMenu_DynamicCommand
        : BaseDynamicCommand<VisualStudioContextMenu_DynamicCommand, VisualStudioContextMenuItem>
    {
        /// <summary>Shared bridge supplying the current menu items and receiving the user's choice.</summary>
        private VisualStudioContextMenuCommandBridge _bridge;

        /// <summary>Resolves the shared <see cref="VisualStudioContextMenuCommandBridge"/> service once the command is initialized.</summary>
        protected override async Task InitializeCompletedAsync()
        {
            _bridge = await FreeAIrPackage.Instance.GetServiceAsync<VisualStudioContextMenuCommandBridge, VisualStudioContextMenuCommandBridge>();
        }

        /// <summary>Returns the menu items currently published by the bridge for this dynamic command to render.</summary>
        protected override IReadOnlyList<VisualStudioContextMenuItem> GetItems()
        {
            return _bridge.MenuItems;
        }

        /// <summary>Applies an item's title, checked and enabled state to its Visual Studio menu command before display.</summary>
        protected override void BeforeQueryStatus(OleMenuCommand menuItem, EventArgs e, VisualStudioContextMenuItem item)
        {
            menuItem.Text = item.Title;
            menuItem.Checked = item.IsChecked;
            menuItem.Enabled = item.IsEnabled;
        }

        /// <summary>Records the item the user clicked on the bridge, so the caller awaiting the menu can resume.</summary>
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
