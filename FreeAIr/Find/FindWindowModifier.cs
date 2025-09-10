using EnvDTE80;
using EnvDTE90;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
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

                        var checkBoxes = new List<CheckBox>();
                        findFilesDialogControl.GetRecursiveByType(ref checkBoxes);

                        var sortedTextBoxes = (
                            from textBox in textBoxes
                            where textBox.Visibility == Visibility.Visible && textBox.ActualHeight >= double.Epsilon
                            let crd = textBox.PointToScreen(new Point(0, 0))
                            orderby crd.Y
                            select textBox
                            ).ToList();
                        var subjectToSearchTextBox = sortedTextBoxes[0];
                        var fileTypesFilterTextBox = sortedTextBoxes[2];

                        var ragCheckBox = CreateUseRAGCheckBox(
                            checkBoxes.FirstOrDefault(),
                            findAllButton,
                            subjectToSearchTextBox,
                            fileTypesFilterTextBox
                            );
                        var naturalSearchButton = CreateNaturalLanguageSearchButton(
                            ragCheckBox,
                            findAllButton,
                            subjectToSearchTextBox,
                            fileTypesFilterTextBox
                            );

                        //put the new button
                        var parent = VisualTreeHelper.GetParent(findAllButton) as WrapPanel;
                        parent.Children.Insert(
                            0,
                            naturalSearchButton
                            );
                        parent.Children.Insert(
                            0,
                            ragCheckBox
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
                excp.ActivityLogException();
            }
        }

        private static CheckBox CreateUseRAGCheckBox(
            CheckBox? styleSourceCheckBox,
            Button findAllButton,
            TextBox subjectToSearchTextBox,
            TextBox fileTypesFilterTextBox
            )
        {
            CheckBox useRAGCheckBox;
            if (styleSourceCheckBox is not null)
            {
                useRAGCheckBox = styleSourceCheckBox.GetType().GetConstructors()[0].Invoke(null) as CheckBox;
                useRAGCheckBox.Style = styleSourceCheckBox?.Style;
                useRAGCheckBox.Template = styleSourceCheckBox?.Template;
            }
            else
            {
                useRAGCheckBox = new CheckBox
                {
                    Style = styleSourceCheckBox?.Style
                };
            }

            useRAGCheckBox.Margin = findAllButton.Margin;
            useRAGCheckBox.VerticalAlignment = findAllButton.VerticalAlignment;
            useRAGCheckBox.VerticalContentAlignment = findAllButton.VerticalContentAlignment;
            useRAGCheckBox.Height = findAllButton.ActualHeight;
            useRAGCheckBox.Content = FreeAIr.Resources.Resources.Use_RAG;
            //userRAGCheckBox.ToolTip = "Use NLO embedding JSON files to narrow down search scope, if the files exists for this solution";
            useRAGCheckBox.ToolTip = "DOES NOT IMPLEMENTED YET";

            useRAGCheckBox.IsEnabled = false;

            return useRAGCheckBox;
        }

        private static Button CreateNaturalLanguageSearchButton(
            CheckBox ragCheckBox,
            Button findAllButton,
            TextBox subjectToSearchTextBox,
            TextBox fileTypesFilterTextBox
            )
        {
            var naturalSearchButton = findAllButton.GetType().GetConstructors()[0].Invoke(null) as Button;
            naturalSearchButton.Margin = findAllButton.Margin;
            naturalSearchButton.VerticalAlignment = findAllButton.VerticalAlignment;
            naturalSearchButton.VerticalContentAlignment = findAllButton.VerticalContentAlignment;
            naturalSearchButton.Height = findAllButton.ActualHeight;
            naturalSearchButton.Content = FreeAIr.Resources.Resources.Find_using_natural_language;
            naturalSearchButton.ToolTip = FreeAIr.Resources.Resources.Find_using_natural_language_in_current;
            naturalSearchButton.Style = findAllButton.Style;

            naturalSearchButton.Click += (sender, e) =>
            {
                DoSearch.SearchAsync(
                    ragCheckBox.IsChecked.GetValueOrDefault(false),
                    fileTypesFilterTextBox.Text,
                    subjectToSearchTextBox.Text
                    )
                    .FileAndForget(nameof(DoSearch.SearchAsync));
            };

            static bool CheckEnabledStatus(TextBox subjectToSearchTextBox)
            {
                return string.IsNullOrEmpty(subjectToSearchTextBox.Text);
            }

            subjectToSearchTextBox.TextChanged += (sender, e) =>
            {
                naturalSearchButton.IsEnabled = !CheckEnabledStatus(subjectToSearchTextBox);
            };

            //do it for first time manually:
            naturalSearchButton.IsEnabled = !CheckEnabledStatus(subjectToSearchTextBox);

            return naturalSearchButton;

        }
    }

}
