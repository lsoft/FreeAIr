using FreeAIr.Agents;
using FreeAIr.Commands.ContextMenu;
using FreeAIr.Commands.File;
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
    public sealed class FindScopeContextMenuCommand
        : Base_ContextMenu_DynamicCommand<FindScopeContextMenuCommand, FindScopeContextMenuItem, FindScopeContextMenuCommandBridge>
    {

        protected override async Task ExecuteAsync(FindScopeContextMenuItem item)
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
