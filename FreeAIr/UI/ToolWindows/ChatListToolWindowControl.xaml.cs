using FreeAIr.Commands.File;
using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.VisualLine.SolutionItem;
using FreeAIr.UI.ViewModels;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    public partial class ChatListToolWindowControl : UserControl
    {
        private readonly ChatListViewModel _viewModel;

        public ChatListToolWindowControl(
            ChatListViewModel viewModel
            )
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;
            
            DataContext = viewModel;

            InitializeComponent();

            viewModel.ContextControlFocus += ViewModel_ContextControlFocus;
            viewModel.PromptControlFocus += ViewModel_PromptControlFocus;
        }

        private void ViewModel_ContextControlFocus()
        {
            FocusContextControl();
        }

        private void ViewModel_PromptControlFocus()
        {
            FocusPromptControl();
        }

        private void ChatListToolWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (e.NewValue is bool)
                {
                    var visible = (bool)e.NewValue;
                    if (visible)
                    {
                        FocusPromptControl();
                    }
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        private void FocusContextControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                ChatControlName.FocusContextControl();
            });
        }

        private void FocusPromptControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                ChatControlName.FocusPromptControl();
            });
        }


        private void ChatListToolWindow_Drop(object sender, DragEventArgs e)
        {
            var solutionItemsPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (solutionItemsPaths is null || solutionItemsPaths.Length == 0)
            {
                return;
            }

            e.Handled = true;

            ThreadHelper.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    await AddMovedFilesAndTheirDescendantsToChatContextAsync(
                        solutionItemsPaths
                        );
                }).FileAndForget(nameof(AddMovedFilesAndTheirDescendantsToChatContextAsync));
        }

        private async System.Threading.Tasks.Task AddMovedFilesAndTheirDescendantsToChatContextAsync(
            string[] solutionItemsPaths
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var children = await GetSolutionItemsWithChildrenAsync(solutionItemsPaths);

            await ApplyFileSupportCommand.AddFilesToContextAsync(
                _viewModel.SelectedChat.Chat,
                children
                );
        }

        public static async System.Threading.Tasks.Task<System.Collections.Generic.List<SolutionItem>> GetSolutionItemsWithChildrenAsync(
            string[] solutionItemsPaths
            )
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();

            var solutionItems = await solution.ProcessDownRecursivelyForAsync(
                item =>
                    solutionItemsPaths.Contains(item.FullPath)
                    ,
                false,
                CancellationToken.None
                );

            var children = await ApplyFileSupportCommand.GetChildrenOfFilesAsync(
                solutionItems.Select(s => s.SolutionItem)
                );
            return children;
        }

    }
}

