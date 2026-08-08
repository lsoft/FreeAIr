using FreeAIr.Helper;
using FreeAIr.UI.InSitu;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.ContextMenu
{
    /// <summary>
    /// The MEF-registered service that drives every dynamic Visual Studio context menu in FreeAIr
    /// (agent, model, support action and recorder pickers). It owns the current menu item list and
    /// the user's choice, and hands both to <see cref="VisualStudioContextMenu_DynamicCommand"/>,
    /// which is the OLE command that Visual Studio actually renders and invokes.
    /// </summary>
    public sealed class VisualStudioContextMenuCommandBridge
    {
        /// <summary>
        /// The <c>IVsUIShell.ShowContextMenu</c> placement flags used for every FreeAIr context
        /// menu: anchored below and right-aligned to the invocation point.
        /// </summary>
        private const uint _showOptions = (uint)(
            __VSSHOWCONTEXTMENUOPTS2.VSCTXMENU_PLACEBOTTOM
            | __VSSHOWCONTEXTMENUOPTS2.VSCTXMENU_RIGHTALIGN
            );

        /// <summary>
        /// The menu item the user picked from the last shown context menu, set by
        /// <see cref="VisualStudioContextMenu_DynamicCommand.Execute"/> when a command is invoked.
        /// </summary>
        public VisualStudioContextMenuItem? ChosenItem
        {
            get;
            set;
        }

        /// <summary>
        /// The items of the context menu currently being shown, read by
        /// <see cref="VisualStudioContextMenu_DynamicCommand.GetItems"/> to populate the dynamic
        /// command list; null when no menu is open.
        /// </summary>
        public List<VisualStudioContextMenuItem> MenuItems
        {
            get;
            private set;
        }

        /// <summary>
        /// Publishes <paramref name="menuItems"/> and asks Visual Studio's shell to pop up the
        /// FreeAIr dynamic context menu at the given screen coordinates, waiting for the user's pick.
        /// Skips the shell call entirely when there is nothing, or only one thing, to choose from.
        /// </summary>
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

        /// <summary>
        /// Convenience overload of the context menu picker for callers that have no notion of a
        /// "checked" state for their items; every item is shown unchecked.
        /// </summary>
        public static Task<TResult?> ShowAsync<TResult>(
            string title,
            List<(string, object)> items,
            System.Windows.Media.Visual? control = null
            )
            where TResult : class
        {
            return ShowAsync<TResult>(
                title,
                items.ConvertAll(t => (t.Item1, false, t.Item2)),
                control
                );
        }

        /// <summary>
        /// The general-purpose entry point used by the agent, model, support and recorder pickers:
        /// builds a single-section menu with a title and the given (label, checked, tag) items,
        /// shows it, and returns the tag of whichever item the user chose.
        /// </summary>
        public static async Task<TResult?> ShowAsync<TResult>(
            string title,
            List<(string, bool, object)> items,
            System.Windows.Media.Visual? control = null
            ) where TResult : class
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var value = await BuildMenuItems()
                    .AddTitle(title)
                    .AddItems(items)
                    .ShowAsync<TResult>()
                    ;

                return value;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return null;
        }

        /// <summary>Starts a fluent builder for a multi-section or otherwise custom context menu, as an alternative to the single-section <see cref="ShowAsync{TResult}(string, List{(string, bool, object)}, System.Windows.Media.Visual)"/> overload.</summary>
        public static MenuItemsBuilder BuildMenuItems()
        {
            return new MenuItemsBuilder();
        }

        /// <summary>Fluent builder that accumulates a title and item groups before showing them as one FreeAIr context menu.</summary>
        public sealed class MenuItemsBuilder
        {
            /// <summary>The menu items accumulated so far, in display order.</summary>
            private readonly List<VisualStudioContextMenuItem> _menuItems = new();

            /// <summary>Adds a non-selectable title entry at the current position in the menu.</summary>
            public MenuItemsBuilder AddTitle(string title)
            {
                _menuItems.Add(new VisualStudioContextMenuItem(title));
                return this;
            }

            /// <summary>Appends a group of (label, checked, tag) selectable items to the menu.</summary>
            public MenuItemsBuilder AddItems(
                List<(string, bool, object)> items
                )
            {
                _menuItems.AddRange(
                    items.ConvertAll(a =>
                        new VisualStudioContextMenuItem(
                            a.Item1,
                            a.Item2,
                            a.Item3
                            )
                        )
                    );
                return this;
            }

            /// <summary>Shows the accumulated menu near <paramref name="control"/> (or the cursor if none given) and returns the tag of the chosen item.</summary>
            public async Task<TResult?> ShowAsync<TResult>(
                System.Windows.Media.Visual? control = null
                ) where TResult : class
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
                    var value = await ShowMenuItemsAsync<TResult>(
                        _menuItems,
                        control
                        );

                    return value;
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();
                }

                return null;
            }

            /// <summary>Resolves the shared <see cref="VisualStudioContextMenuCommandBridge"/> MEF service and asks it to pop up the given menu items at the control's (or cursor's) screen position, temporarily allowing in-situ chat input suppression to be lifted so the popup can receive input.</summary>
            private static async Task<TResult?> ShowMenuItemsAsync<TResult>(
                List<VisualStudioContextMenuItem> menuItems,
                System.Windows.Media.Visual? control = null
                ) where TResult : class
            {
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

                var state = InSituChatInputCommandFilter.GetSuppressMode();
                try
                {
                    InSituChatInputCommandFilter.SetSuppressMode(false);

                    var menuItem = await bridge.ShowAsync(
                        menuItems,
                        (int)point.X,
                        (int)point.Y
                        );

                    if (menuItem is not null)
                    {
                        return menuItem.Tag as TResult;
                    }

                    return null;
                }
                finally
                {
                    InSituChatInputCommandFilter.SetSuppressMode(state);
                }
            }

        }
    }

}
