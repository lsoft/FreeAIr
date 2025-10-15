using EnvDTE80;
using EnvDTE90;
using FreeAIr.Helper;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
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
                    var telemetry = new FindAllTelemetryS();
                    telemetry.AddStep("start");

                    try
                    {
                        foreach (System.Windows.Window w in Application.Current.Windows)
                        {
                            telemetry.AddStep("first line in foreach");

                            var findFilesDialogControl = w.GetRecursiveByTypeOrName("FindFilesDialogControl", null);
                            if (findFilesDialogControl == null)
                            {
                                continue;
                            }

                            telemetry.AddStep("after continue FindFilesDialogControl");

                            var findAllButton = findFilesDialogControl.GetRecursiveByName("FindAll") as Button;
                            if (findAllButton == null)
                            {
                                continue;
                            }

                            telemetry.AddStep("after continue FindAll");

                            var textBoxes = new List<TextBox>();
                            findFilesDialogControl.GetRecursiveByType(ref textBoxes);

                            telemetry.AddStep($"after searching textboxes, found: {textBoxes.Count}");
                            textBoxes.ForEach(tb => telemetry.AddTextBox(tb));

                            var checkBoxes = new List<CheckBox>();
                            findFilesDialogControl.GetRecursiveByType(ref checkBoxes);

                            telemetry.AddStep($"after searching checkBoxes, found: {checkBoxes.Count}");
                            checkBoxes.ForEach(cb => telemetry.AddCheckBox(cb));

                            var sortedTextBoxes = (
                                from textBox in textBoxes
                                where textBox.Visibility == Visibility.Visible && textBox.ActualHeight >= double.Epsilon
                                let crd = textBox.PointToScreen(new Point(0, 0))
                                orderby crd.Y
                                select textBox
                                ).ToList();

                            telemetry.AddStep($"after sorting textBoxes, found: {sortedTextBoxes.Count}");
                            sortedTextBoxes.ForEach(tb => telemetry.AddSortedTextBox(tb));

                            var subjectToSearchTextBox = sortedTextBoxes[0];
                            var fileTypesFilterTextBox = sortedTextBoxes[2];

                            var ragCheckBox = CreateUseRAGCheckBox(
                                checkBoxes.FirstOrDefault(),
                                findAllButton,
                                subjectToSearchTextBox,
                                fileTypesFilterTextBox
                                );

                            telemetry.AddStep($"after ragCheckBox");

                            var naturalSearchButton = CreateNaturalLanguageSearchButton(
                                ragCheckBox,
                                findAllButton,
                                subjectToSearchTextBox,
                                fileTypesFilterTextBox
                                );

                            telemetry.AddStep($"after naturalSearchButton");

                            //put the new button
                            var parent = VisualTreeHelper.GetParent(findAllButton) as WrapPanel;

                            if (parent is not null)
                                telemetry.AddStep($"after find parent, parent is NOT null");
                            else
                                telemetry.AddStep($"after find parent, parent is null");

                            parent.Children.Insert(
                                0,
                                naturalSearchButton
                                );

                            telemetry.AddStep($"after inserting naturalSearchButton");

                            parent.Children.Insert(
                                0,
                                ragCheckBox
                                );

                            telemetry.AddStep($"SUCCESS");

                            return;
                        }
                    }
                    catch (Exception excp)
                    {
                        telemetry.AddStep($"FAIL");
                        telemetry.AddStep(excp.Message);
                        telemetry.AddStep(excp.StackTrace);
                    }
                    finally
                    {
                        var serialized = JsonConvert.SerializeObject(telemetry, Formatting.Indented);
                        ActivityLog.LogError(
                            "FreeAIr (Find Window)",
                            serialized
                            );
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

    public class FindAllTelemetryS
    {
        public List<string> Steps
        {
            get;
            set;
        }
        public void AddStep(string step)
        {
            Steps.Add(step);
        }

        public List<TextBoxS> TextBoxes
        {
            get;
            set;
        }

        public List<CheckBoxS> CheckBoxes
        {
            get;
            set;
        }

        public List<TextBoxS> SortedTextBoxes
        {
            get;
            set;
        }

        public void AddTextBox(TextBox textBox)
        {
            var position = textBox.PointToScreen(new Point(0, 0));

            TextBoxes.Add(
                new TextBoxS
                {
                    Name = textBox?.Name ?? "NULL",
                    Content = textBox?.Text ?? "NULL",
                    X = position.X,
                    Y = position.Y,
                    IsEnabled = textBox.IsEnabled
                }
                );
        }

        public void AddSortedTextBox(TextBox textBox)
        {
            var position = textBox.PointToScreen(new Point(0, 0));

            SortedTextBoxes.Add(
                new TextBoxS
                {
                    Name = textBox?.Name ?? "NULL",
                    Content = textBox?.Text ?? "NULL",
                    X = position.X,
                    Y = position.Y,
                    IsEnabled = textBox.IsEnabled
                }
                );
        }

        public void AddCheckBox(CheckBox checkBox)
        {
            var position = checkBox.PointToScreen(new Point(0, 0));

            CheckBoxes.Add(
                new CheckBoxS
                {
                    Name = checkBox?.Name ?? "NULL",
                    Content = checkBox.Content?.ToString() ?? "NULL",
                    X = position.X,
                    Y = position.Y,
                    IsEnabled = checkBox.IsEnabled,
                    IsChecked = checkBox.IsChecked
                }
                );
        }

        public FindAllTelemetryS()
        {
            Steps = new();
            TextBoxes = new();
            CheckBoxes = new();
            SortedTextBoxes = new();
        }
    }

    public class TextBoxS
    {
        public string Name
        {
            get;
            set;
        }

        public string Content
        {
            get;
            set;
        }

        public double X
        {
            get;
            set;
        }

        public double Y
        {
            get;
            set;
        }

        public bool IsEnabled
        {
            get;
            set;
        }
    }

    public class CheckBoxS
    {
        public string Name
        {
            get;
            set;
        }

        public string Content
        {
            get;
            set;
        }

        public bool? IsChecked
        {
            get;
            set;
        }

        public double X
        {
            get;
            set;
        }

        public double Y
        {
            get;
            set;
        }

        public bool IsEnabled
        {
            get;
            set;
        }
    }

}
