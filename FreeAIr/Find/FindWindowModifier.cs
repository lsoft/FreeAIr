using EnvDTE80;
using FreeAIr.Agents;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

                        var sortedTextBoxes = (
                            from textBox in textBoxes
                            where textBox.GetType() == typeof(TextBox)
                            where textBox.Visibility == Visibility.Visible && textBox.ActualHeight >= double.Epsilon
                            let crd = textBox.PointToScreen(new Point(0, 0))
                            orderby crd.Y
                            select textBox
                            ).ToList();
                        var subjectToSearchTextBox = sortedTextBoxes[0];
                        var fileTypesFilterTextBox = sortedTextBoxes[2];

                        var naturalSearchButton = new Button
                        {
                            Margin = findAllButton.Margin,
                            VerticalAlignment = findAllButton.VerticalAlignment,
                            VerticalContentAlignment = findAllButton.VerticalContentAlignment,
                            Height = findAllButton.ActualHeight,
                            Content = "Find using natural language",
                            ToolTip = "Find using natural language in every file of current solution",
                            Style = findAllButton.Style
                        };

                        naturalSearchButton.Click += (sender, e) =>
                        {
                            DoSearch.SearchAsync(
                                fileTypesFilterTextBox.Text,
                                subjectToSearchTextBox.Text
                                )
                                .FileAndForget(nameof(DoSearch.SearchAsync));
                        };

                        subjectToSearchTextBox.TextChanged += (sender, e) =>
                        {
                            naturalSearchButton.IsEnabled = !string.IsNullOrEmpty(subjectToSearchTextBox.Text);
                        };

                        //do it for first time manually:
                        naturalSearchButton.IsEnabled = !string.IsNullOrEmpty(subjectToSearchTextBox.Text);

                        //put the new button
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
    }

}
