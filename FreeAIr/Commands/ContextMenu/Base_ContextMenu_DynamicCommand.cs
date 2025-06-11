using FreeAIr.Agents;
using FreeAIr.Commands.File;
using FreeAIr.UI.Bridge;
using System.Collections.Generic;

namespace FreeAIr.Commands.ContextMenu
{
    public abstract class Base_ContextMenu_DynamicCommand<TCommand, TItem, TBridge> : BaseDynamicCommand<TCommand, TItem>
        where TCommand : Base_ContextMenu_DynamicCommand<TCommand, TItem, TBridge>, new()
        where TBridge : MenuCommandBridge<TItem>
    {

        protected TBridge _bridge;

        protected override async Task InitializeCompletedAsync()
        {
            _bridge = await FreeAIrPackage.Instance.GetServiceAsync<TBridge, TBridge>();
        }

        protected override IReadOnlyList<TItem> GetItems()
        {
            return _bridge.MenuItems;
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuItem, EventArgs e, TItem item)
        {
            menuItem.Text = item.ToString();
            menuItem.Checked = false;
        }

        protected override async void Execute(OleMenuCmdEventArgs e, TItem item)
        {
            try
            {
                await ExecuteAsync(item);
            }
            catch (Exception excp)
            {
                int g = 0;
            }
        }

        protected abstract Task ExecuteAsync(TItem agent);
    }

}
