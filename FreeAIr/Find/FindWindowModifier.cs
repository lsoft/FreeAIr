using EnvDTE;
using EnvDTE80;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.Find
{
    public static class FindWindowModifier
    {
        public static async Task StartScanAsync(CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await FreeAIrPackage.Instance.GetServiceAsync(typeof(EnvDTE.DTE)) as DTE2;

                while (true)
                {
                    foreach (System.Windows.Window w in Application.Current.Windows)
                    {
                        var findFilesDialogControl = w.GetRecursiveByTypeOrName("FindFilesDialogControl", null);
                        if (findFilesDialogControl == null)
                        {
                            continue;
                        }

                        var findAllButton = findFilesDialogControl.GetRecursiveByName("FindAll") as Button;
                        if (findAllButton == null)
                        {
                            continue;
                        }

                        var textBoxes = new List<TextBox>();
                        findFilesDialogControl.GetRecursiveByType(ref textBoxes);

                        var subjectToSearchTextBox = (
                            from textBox in textBoxes
                            where textBox.GetType() == typeof(TextBox)
                            where textBox.Visibility == Visibility.Visible && textBox.ActualHeight >= double.Epsilon
                            let crd = textBox.PointToScreen(new Point(0, 0))
                            orderby crd.Y
                            select textBox
                            ).FirstOrDefault();

                        var naturalSearchButton = new Button
                        {
                            Content = new TextBlock
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Text = "Find using natural language (whole solution)"
                            },
                            Margin = findAllButton.Margin,
                            VerticalAlignment = findAllButton.VerticalAlignment,
                            VerticalContentAlignment = findAllButton.VerticalContentAlignment,
                            Height = findAllButton.ActualHeight,
                            Style = findAllButton.Style,
                            ToolTip = "Find using natural language in every file of current solution (filters do not applied)"
                        };
                        naturalSearchButton.Click += (sender, e) =>
                        {
                            StartSearchByNaturalLanguageAsync(subjectToSearchTextBox.Text)
                                .FileAndForget(nameof(StartSearchByNaturalLanguageAsync));
                        };

                        subjectToSearchTextBox.TextChanged += (sender, e) =>
                        {
                            naturalSearchButton.IsEnabled = !string.IsNullOrEmpty(subjectToSearchTextBox.Text);
                        };

                        //do it for first time manually:
                        naturalSearchButton.IsEnabled = !string.IsNullOrEmpty(subjectToSearchTextBox.Text);

                        //but the new button
                        var parent = VisualTreeHelper.GetParent(findAllButton) as WrapPanel;
                        parent.Children.Insert(
                            0,
                            naturalSearchButton
                            );
                        return;
                    }

                    await Task.Delay(250, ct);

                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //nothing to do
            }
            catch (Exception excp)
            {
                //todo logging
            }
        }

        private static async Task StartSearchByNaturalLanguageAsync(
            string searchText
            )
        {
            //закрываем окно поиска
            CloseFindWindow();

            var pane = await NaturalLanguageResultsToolWindow.ShowAsync();
            var toolWindow = pane.Content as NaturalLanguageResultsToolWindowControl;
            var viewModel = toolWindow.DataContext as NaturalLanguageResultsViewModel;
            await viewModel.SetNewChatAsync(searchText);
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
