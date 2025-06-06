using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.Bridge
{
    public abstract class MenuCommandBridge<TMenuItemType>
    {
        private const uint _showOptions = (uint)(__VSSHOWCONTEXTMENUOPTS2.VSCTXMENU_PLACEBOTTOM | __VSSHOWCONTEXTMENUOPTS2.VSCTXMENU_RIGHTALIGN);

        protected abstract int GetMenuID();

        public List<TMenuItemType> MenuItems
        {
            get;
            private set;
        }

        public async Task ShowAsync(List<TMenuItemType> menuItems, int x, int y)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            MenuItems = menuItems;

            try
            {
                IVsUIShell shell = await VS.Services.GetUIShellAsync();

                POINTS[] locationPoints = new[]
                {
                    new POINTS
                    {
                        x = (short)x,
                        y = (short)y
                    }
                };

                _ = shell.ShowContextMenu(
                    _showOptions,
                    PackageGuids.FreeAIr,
                    GetMenuID(),
                    locationPoints,
                    pCmdTrgtActive: null
                    );
            }
            finally
            {
                MenuItems = null;
            }
        }

        public static void ShowContextMenu<T>(
            System.Windows.Media.Visual control,
            List<TMenuItemType> menuItems
            )
            where T : MenuCommandBridge<TMenuItemType>
        {
            ThreadHelper.JoinableTaskFactory.Run(
                async () =>
                {
                    var bridge = await VS.GetRequiredServiceAsync<T, T>();
                    var point = control.PointToScreen(new Point(0, 0));
                    await bridge.ShowAsync(menuItems, (int)point.X, (int)point.Y);
                });
        }
    }
}
