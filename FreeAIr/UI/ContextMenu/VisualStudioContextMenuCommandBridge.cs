using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.ContextMenu
{
    public sealed class VisualStudioContextMenuCommandBridge
    {
        private const uint _showOptions = (uint)(
            __VSSHOWCONTEXTMENUOPTS2.VSCTXMENU_PLACEBOTTOM
            | __VSSHOWCONTEXTMENUOPTS2.VSCTXMENU_RIGHTALIGN
            );

        public VisualStudioContextMenuItem? ChosenItem
        {
            get;
            set;
        }

        public List<VisualStudioContextMenuItem> MenuItems
        {
            get;
            private set;
        }

        private async Task<VisualStudioContextMenuItem?> ShowAsync(
            List<VisualStudioContextMenuItem> menuItems,
            int x,
            int y
            )
        {
            if (menuItems.Count == 0)
            {
                return null;
            }
            if (menuItems.Count == 1)
            {
                return menuItems[0];
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ChosenItem = null;
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

                _ = (Microsoft.VisualStudio.OLE.Interop.Constants)shell.ShowContextMenu(
                    _showOptions,
                    PackageGuids.FreeAIr,
                    PackageIds.VisualStudioContextMenu,
                    locationPoints,
                    pCmdTrgtActive: null
                    );
            }
            finally
            {
                MenuItems = null;
            }

            return ChosenItem;
        }

        public static async Task<TResult?> ShowAsync<TResult>(
            string title,
            List<(string, object)> items,
            System.Windows.Media.Visual? control = null
            )
            where TResult : class
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var menuItems = BuildMenuItems(title, items);

                var bridge = await VS.GetRequiredServiceAsync<VisualStudioContextMenuCommandBridge, VisualStudioContextMenuCommandBridge>();

                Point point;
                if (control is null)
                {
                    point = new System.Windows.Point(
                        System.Windows.Forms.Cursor.Position.X,
                        System.Windows.Forms.Cursor.Position.Y
                        );
                }
                else
                {
                    point = control.PointToScreen(new Point(0, 0));
                }

                var menuItem = await bridge.ShowAsync(
                    menuItems,
                    (int)point.X,
                    (int)point.Y
                    );

                if (menuItem is not null)
                {
                    return menuItem.Tag as TResult;
                }
            }
            catch (Exception excp)
            {
                //todo log
            }

            return null;
        }

        private static List<VisualStudioContextMenuItem> BuildMenuItems(
            string title,
            List<(string, object)> items
            )
        {
            List<VisualStudioContextMenuItem> menuItems = new();

            menuItems.Add(
                new VisualStudioContextMenuItem(
                    title
                    )
                );

            menuItems.AddRange(
                items.ConvertAll(a =>
                    new VisualStudioContextMenuItem(
                        a.Item1,
                        a.Item2
                        )
                    )
                );

            return menuItems;
        }
    }


}
