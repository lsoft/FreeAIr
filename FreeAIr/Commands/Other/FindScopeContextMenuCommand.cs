using FreeAIr.Find;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
using System.Windows;
using static FreeAIr.Find.FindScopeContextMenuCommandBridge;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.FindScopeContextMenuDynamicCommandId)]
    public sealed class FindScopeContextMenuCommand : BaseDynamicCommand<FindScopeContextMenuCommand, FindScopeContextMenuItem>
    {
        private FindScopeContextMenuCommandBridge _bridge;

        protected override async Task InitializeCompletedAsync()
        {
            _bridge = await FreeAIrPackage.Instance.GetServiceAsync<FindScopeContextMenuCommandBridge, FindScopeContextMenuCommandBridge>();
        }

        protected override IReadOnlyList<FindScopeContextMenuItem> GetItems()
        {
            return _bridge.MenuItems;
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuItem, EventArgs e, FindScopeContextMenuItem item)
        {
            menuItem.Text = item.Header;
            menuItem.Checked = false;
        }

        protected override async void Execute(OleMenuCmdEventArgs e, FindScopeContextMenuItem item)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var fileTypesFilter = _bridge.FileTypesFilterText;
                var filesTypeFilters = new FileTypesFilter(
                    fileTypesFilter
                        .Split(';')
                        .ConvertAll(f => new FileTypeFilter(f))
                    );

                var scope = item.Scope;

                //закрываем окно поиска
                CloseFindWindow();

                var pane = await NaturalLanguageResultsToolWindow.ShowAsync();
                var toolWindow = pane.Content as NaturalLanguageResultsToolWindowControl;
                var viewModel = toolWindow.DataContext as NaturalLanguageResultsViewModel;
                viewModel.SetNewChatAsync(scope, _bridge.SubjectToSearchText, filesTypeFilters)
                    .FileAndForget(nameof(NaturalLanguageResultsViewModel.SetNewChatAsync));
            }
            catch (Exception excp)
            {
                int g = 0;
            }
        }

        private static void CloseFindWindow()
        {
            foreach (System.Windows.Window window in Application.Current.Windows)
            {
                if (window == Application.Current.MainWindow)
                {
                    continue;
                }

                if (window.IsActive)
                {
                    window.Close();
                }
            }
        }

    }
}
