using FreeAIr.Antlr.Context;
using FreeAIr.Antlr.Prompt;
using FreeAIr.Commands.File;
using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.VisualLine.Command;
using FreeAIr.UI.Embedillo.VisualLine.SolutionItem;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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

            SetupPromptControl();

            SetupAddToContextControl();

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
                //todo log
            }
        }

        private void FocusContextControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                AddToContextControl.MakeFocused();
            });

            //await Task.Delay(100);
            //AddToContextControl.MakeFocused();
        }

        private void FocusPromptControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                PromptControl.MakeFocused();
            });

            //await Task.Delay(100);
            //PromptControl.MakeFocused();
        }

        private void SetupAddToContextControl()
        {
            AddToContextControl.Setup(
                new ContextParser(
                    new SolutionItemVisualLineGeneratorFactory()
                    )
                );
        }

        private void SetupPromptControl()
        {
            PromptControl.Setup(
                new PromptParser(
                    new SolutionItemVisualLineGeneratorFactory(),
                    new CommandVisualLineGeneratorFactory()
                    )
                );
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

        private void EmbedilloControl_Drop(object sender, DragEventArgs e)
        {
            var solutionItemsPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (solutionItemsPaths is null || solutionItemsPaths.Length == 0)
            {
                return;
            }

            var embedillo = sender as Embedillo.EmbedilloControl;
            if (embedillo is null)
            {
                return;
            }

            e.Handled = true;

            AddMovedFilesAndTheirDescendantsToChatPromptAsync(
                embedillo,
                solutionItemsPaths
                ).FileAndForget(nameof(AddMovedFilesAndTheirDescendantsToChatPromptAsync));
        }

        private async System.Threading.Tasks.Task AddMovedFilesAndTheirDescendantsToChatPromptAsync(
            Embedillo.EmbedilloControl embedillo,
            string[] solutionItemsPaths
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var children = await GetSolutionItemsWithChildrenAsync(solutionItemsPaths);

            foreach (var child in children)
            {
                if (
                    !string.IsNullOrEmpty(embedillo.AvalonTextEditor.Text)
                    && !char.IsWhiteSpace(embedillo.AvalonTextEditor.Text[embedillo.AvalonTextEditor.Text.Length - 1]))
                {
                    embedillo.AvalonTextEditor.Text += " ";
                }

                embedillo.AvalonTextEditor.Text += SolutionItemVisualLineGenerator.Anchor + child.FullPath;
            }

            embedillo.AvalonTextEditor.CaretOffset = embedillo.AvalonTextEditor.Text.Length;

            embedillo.UpdateHintStatus();
        }

        private static async System.Threading.Tasks.Task<System.Collections.Generic.List<SolutionItem>> GetSolutionItemsWithChildrenAsync(
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

    public class RelativeMaxHeightConverter : IValueConverter
    {
        public double Ratio { get; set; } = 0.2; // 20% от высоты контейнера по умолчанию

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                return height * Ratio; // Возвращаем относительную высоту
            }
            return double.NaN;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

