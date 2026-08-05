using FreeAIr.Commands.File;
using FreeAIr.Helper;
using FreeAIr.UI.ViewModels;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// Code-behind for the AI chat list tool window; wires up focus events between the view
    /// model and the chat control, and handles files dropped from Solution Explorer by adding
    /// them (and their descendants) to the selected chat's context.
    /// </summary>
    public partial class ChatListToolWindowControl : UserControl
    {
        private readonly ChatListViewModel _viewModel;

        /// <summary>
        /// Creates the control, binds it to the chat list view model, and subscribes to the
        /// view model's requests to move keyboard focus to the context or prompt editors.
        /// </summary>
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

        /// <summary>
        /// Moves keyboard focus to the chat's context editor in response to the view model.
        /// </summary>
        private void ViewModel_ContextControlFocus()
        {
            FocusContextControl();
        }

        /// <summary>
        /// Moves keyboard focus to the chat's prompt editor in response to the view model.
        /// </summary>
        private void ViewModel_PromptControlFocus()
        {
            FocusPromptControl();
        }

        /// <summary>
        /// Focuses the prompt editor whenever the tool window becomes visible, so the user can
        /// start typing immediately after switching to the chat list.
        /// </summary>
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

        /// <summary>
        /// Asynchronously moves keyboard focus to the chat context editor.
        /// </summary>
        private void FocusContextControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                ChatControlName.FocusContextControl();
            });
        }

        /// <summary>
        /// Asynchronously moves keyboard focus to the chat prompt editor.
        /// </summary>
        private void FocusPromptControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                ChatControlName.FocusPromptControl();
            });
        }


        /// <summary>
        /// Handles files dropped onto the chat list from Solution Explorer or the OS, adding the
        /// dropped items and their descendants to the currently selected chat's file context.
        /// </summary>
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

        /// <summary>
        /// Resolves the dropped paths to solution items (including their child files) and adds
        /// them all to the selected chat's context via <see cref="ApplyFileSupportCommand"/>.
        /// </summary>
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

        /// <summary>
        /// Finds the solution items matching the given file paths and expands them to include
        /// every descendant file, so a dropped folder or project pulls in all of its contents.
        /// </summary>
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

