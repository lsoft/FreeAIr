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
    /// <summary>
    /// Injects the `Find using natural language` button and the `Use RAG` checkbox into Visual
    /// Studio's built-in "Find in Files" dialog. The dialog is not FreeAIr's own window, so its
    /// controls have to be located by walking the visual tree and patched in after the fact, and the
    /// patch has to be repeated every time the dialog is reopened since it is rebuilt from scratch.
    /// </summary>
    public static class FindWindowModifier
    {
        /// <summary>
        /// Put into <see cref="FrameworkElement.Tag"/> of the controls this class inserts, so that
        /// a repeated scan recognizes its own work and does not add a second set of them.
        /// </summary>
        private const string NaturalSearchButtonTag = "FreeAIr.NaturalLanguageSearch";
        /// <summary>
        /// The <see cref="FrameworkElement.Tag"/> marking the injected `Use RAG` checkbox.
        /// </summary>
        private const string UseRagCheckBoxTag = "FreeAIr.UseRAG";

        /// <summary>
        /// Remembered so that the scan can be restarted when the dialog it has patched goes away.
        /// </summary>
        private static CancellationToken _cancellationToken;

        /// <summary>
        /// Polls the open windows for the `Find in Files` dialog and, once found, inserts the
        /// natural language search button and the `Use RAG` checkbox next to its `Find All` button.
        /// Keeps polling until the dialog appears or the token is cancelled, and rearms itself
        /// through <see cref="WatchForDialogTeardown"/> so a later reopening of the dialog is
        /// patched again too.
        /// </summary>
        public static async Task StartScanAsync(CancellationToken ct)
        {
            _cancellationToken = ct;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await FreeAIrPackage.Instance.GetServiceAsync(typeof(EnvDTE.DTE)) as DTE2;

                while (true)
                {
                    //var telemetry = new FindAllTelemetryS();
                    //telemetry.AddStep("start");

                    //try
                    //{
                    foreach (System.Windows.Window w in Application.Current.Windows)
                    {
                        //telemetry.AddStep("first line in foreach");

                        var findFilesDialogControl = w.GetRecursiveByTypeOrName("FindFilesDialogControl", null);
                        if (findFilesDialogControl == null)
                        {
                            continue;
                        }

                        //telemetry.AddStep("after continue FindFilesDialogControl");

                        var findAllButton = findFilesDialogControl.GetRecursiveByName("FindAll") as Button;
                        if (findAllButton == null)
                        {
                            continue;
                        }
                        if (findAllButton.Visibility != Visibility.Visible)
                        {
                            continue;
                        }

                        //telemetry.AddStep("after continue FindAll");

                        //put the new button
                        var parent = VisualTreeHelper.GetParent(findAllButton) as WrapPanel;
                        if (parent is null)
                        {
                            continue;
                        }

                        var alreadyInsertedButton = FindOwnControl(parent, NaturalSearchButtonTag);
                        if (alreadyInsertedButton is not null)
                        {
                            //диалог уже дополнен: второй комплект контролов ему не нужен
                            WatchForDialogTeardown(alreadyInsertedButton);
                            return;
                        }

                        var textBoxes = new List<TextBox>();
                        findFilesDialogControl.GetRecursiveByType(ref textBoxes);

                        //telemetry.AddStep($"after searching textboxes, found: {textBoxes.Count}");
                        //textBoxes.ForEach(tb => telemetry.AddTextBox(tb));

                        var checkBoxes = new List<CheckBox>();
                        findFilesDialogControl.GetRecursiveByType(ref checkBoxes);

                        //telemetry.AddStep($"after searching checkBoxes, found: {checkBoxes.Count}");
                        //checkBoxes.ForEach(cb => telemetry.AddCheckBox(cb));

                        var sortedTextBoxes = (
                            from textBox in textBoxes
                            where textBox.Visibility == Visibility.Visible && textBox.ActualHeight >= double.Epsilon
                            let crd = textBox.PointToScreen(new Point(0, 0))
                            orderby crd.Y
                            select textBox
                            ).ToList();

                        //telemetry.AddStep($"after sorting textBoxes, found: {sortedTextBoxes.Count}");
                        //sortedTextBoxes.ForEach(tb => telemetry.AddSortedTextBox(tb));

                        var subjectToSearchTextBox = sortedTextBoxes[0];
                        var fileTypesFilterTextBox = sortedTextBoxes[2];

                        var ragCheckBox = CreateUseRAGCheckBox(
                            checkBoxes.FirstOrDefault(),
                            findAllButton,
                            subjectToSearchTextBox,
                            fileTypesFilterTextBox
                            );

                        //telemetry.AddStep($"after ragCheckBox");

                        var naturalSearchButton = CreateNaturalLanguageSearchButton(
                            ragCheckBox,
                            findAllButton,
                            subjectToSearchTextBox,
                            fileTypesFilterTextBox
                            );

                        //telemetry.AddStep($"after naturalSearchButton");

                        parent.Children.Insert(
                            0,
                            naturalSearchButton
                            );

                        //telemetry.AddStep($"after inserting naturalSearchButton");

                        parent.Children.Insert(
                            0,
                            ragCheckBox
                            );

                        //telemetry.AddStep($"SUCCESS");

                        WatchForDialogTeardown(naturalSearchButton);
                        return;
                    }
                    //}
                    //catch (Exception excp)
                    //{
                    //    telemetry.AddStep($"FAIL");
                    //    telemetry.AddStep(excp.Message);
                    //    telemetry.AddStep(excp.StackTrace);
                    //}
                    //finally
                    //{
                    //    var serialized = JsonConvert.SerializeObject(telemetry, Formatting.Indented);
                    //    ActivityLog.LogError(
                    //        "FreeAIr (Find Window)",
                    //        serialized
                    //        );
                    //}

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

        /// <summary>
        /// Looks for a control this class has already inserted into the given panel, identified by
        /// its <see cref="FrameworkElement.Tag"/>, so a repeated scan of the same dialog does not
        /// insert a duplicate.
        /// </summary>
        private static FrameworkElement? FindOwnControl(
            WrapPanel panel,
            string tag
            )
        {
            foreach (var child in panel.Children)
            {
                if (child is FrameworkElement element && (element.Tag as string) == tag)
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>
        /// "Find in Files" is a dialog: it is built anew every time the user opens it, so the
        /// controls inserted into it live exactly as long as one dialog does. The scan stops as
        /// soon as it has patched the dialog, so without this the second and every further opening
        /// of the dialog would come without the natural language search.
        /// </summary>
        private static void WatchForDialogTeardown(
            FrameworkElement ownControl
            )
        {
            ownControl.Unloaded -= OwnControlUnloaded;
            ownControl.Unloaded += OwnControlUnloaded;
        }

        /// <summary>
        /// Fires when the injected control is unloaded along with the closing `Find in Files`
        /// dialog, and restarts <see cref="StartScanAsync"/> so the next time the dialog opens it
        /// gets patched again.
        /// </summary>
        private static void OwnControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Unloaded -= OwnControlUnloaded;
            }

            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            StartScanAsync(_cancellationToken)
                .FileAndForget(nameof(FindWindowModifier));
        }

        /// <summary>
        /// Builds the `Use RAG` checkbox inserted next to the dialog's `Find All` button, styled to
        /// match it and initially disabled until <see cref="EnableUseRAGCheckBoxAsync"/> confirms an
        /// embedding index exists to search.
        /// </summary>
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

            findAllButton.IsVisibleChanged += (sender, e) =>
            {
                useRAGCheckBox.Visibility = findAllButton.Visibility;
            };

            useRAGCheckBox.Tag = UseRagCheckBoxTag;
            useRAGCheckBox.Margin = findAllButton.Margin;
            useRAGCheckBox.VerticalAlignment = findAllButton.VerticalAlignment;
            useRAGCheckBox.VerticalContentAlignment = findAllButton.VerticalContentAlignment;
            useRAGCheckBox.Height = findAllButton.ActualHeight;
            useRAGCheckBox.Content = FreeAIr.Resources.Resources.Use_RAG;

            //the checkbox starts disabled and is enabled below if an index turns up: reading the
            //solution folder cannot be done synchronously here, and offering a search which has
            //nothing to search in is worse than a checkbox which lights up a moment later
            useRAGCheckBox.IsEnabled = false;
            useRAGCheckBox.ToolTip = FreeAIr.Resources.Resources.RAG__there_is_no_index_short;

            EnableUseRAGCheckBoxAsync(useRAGCheckBox)
                .FileAndForget(nameof(EnableUseRAGCheckBoxAsync));

            return useRAGCheckBox;
        }

        /// <summary>
        /// Enables the `Use RAG` checkbox when the solution has an embedding index, and tells the
        /// user how old that index is — a search is only as good as the index behind it, and an
        /// index built a month ago knows nothing about anything written since.
        /// </summary>
        private static async Task EnableUseRAGCheckBoxAsync(
            CheckBox useRAGCheckBox
            )
        {
            var metadata = await Embedding.EmbeddingIndexContainer.TryReadMetadataAsync();
            if (metadata is null)
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            useRAGCheckBox.IsEnabled = true;
            useRAGCheckBox.ToolTip =
                FreeAIr.Resources.Resources.RAG__narrows_the_search_down
                + Environment.NewLine
                + string.Format(
                    FreeAIr.Resources.Resources.RAG__the_index_has_been_built__0_,
                    metadata.GenerateDateTime.ToString("g")
                    )
                ;
        }

        /// <summary>
        /// Builds the `Find using natural language` button inserted next to the dialog's `Find All`
        /// button, styled to match it and wired to call <see cref="DoSearch.SearchAsync"/> with the
        /// query and file mask text boxes and the state of the `Use RAG` checkbox.
        /// </summary>
        private static Button CreateNaturalLanguageSearchButton(
            CheckBox ragCheckBox,
            Button findAllButton,
            TextBox subjectToSearchTextBox,
            TextBox fileTypesFilterTextBox
            )
        {
            var naturalSearchButton = findAllButton.GetType().GetConstructors()[0].Invoke(null) as Button;
            naturalSearchButton.Tag = NaturalSearchButtonTag;
            naturalSearchButton.Margin = findAllButton.Margin;
            naturalSearchButton.VerticalAlignment = findAllButton.VerticalAlignment;
            naturalSearchButton.VerticalContentAlignment = findAllButton.VerticalContentAlignment;
            naturalSearchButton.Height = findAllButton.ActualHeight;
            naturalSearchButton.Content = FreeAIr.Resources.Resources.Find_using_natural_language;
            naturalSearchButton.ToolTip = FreeAIr.Resources.Resources.Find_using_natural_language_in_current;
            naturalSearchButton.Style = findAllButton.Style;

            findAllButton.IsVisibleChanged += (sender, e) =>
            {
                naturalSearchButton.Visibility = findAllButton.Visibility;
            };

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

    //public class FindAllTelemetryS
    //{
    //    public List<string> Steps
    //    {
    //        get;
    //        set;
    //    }
    //    public void AddStep(string step)
    //    {
    //        Steps.Add(step);
    //    }

    //    public List<TextBoxS> TextBoxes
    //    {
    //        get;
    //        set;
    //    }

    //    public List<CheckBoxS> CheckBoxes
    //    {
    //        get;
    //        set;
    //    }

    //    public List<TextBoxS> SortedTextBoxes
    //    {
    //        get;
    //        set;
    //    }

    //    public void AddTextBox(TextBox textBox)
    //    {
    //        var position = textBox.PointToScreen(new Point(0, 0));

    //        TextBoxes.Add(
    //            new TextBoxS
    //            {
    //                Name = textBox?.Name ?? "NULL",
    //                Content = textBox?.Text ?? "NULL",
    //                X = position.X,
    //                Y = position.Y,
    //                IsEnabled = textBox.IsEnabled
    //            }
    //            );
    //    }

    //    public void AddSortedTextBox(TextBox textBox)
    //    {
    //        var position = textBox.PointToScreen(new Point(0, 0));

    //        SortedTextBoxes.Add(
    //            new TextBoxS
    //            {
    //                Name = textBox?.Name ?? "NULL",
    //                Content = textBox?.Text ?? "NULL",
    //                X = position.X,
    //                Y = position.Y,
    //                IsEnabled = textBox.IsEnabled
    //            }
    //            );
    //    }

    //    public void AddCheckBox(CheckBox checkBox)
    //    {
    //        var position = checkBox.PointToScreen(new Point(0, 0));

    //        CheckBoxes.Add(
    //            new CheckBoxS
    //            {
    //                Name = checkBox?.Name ?? "NULL",
    //                Content = checkBox.Content?.ToString() ?? "NULL",
    //                X = position.X,
    //                Y = position.Y,
    //                IsEnabled = checkBox.IsEnabled,
    //                IsChecked = checkBox.IsChecked
    //            }
    //            );
    //    }

    //    public FindAllTelemetryS()
    //    {
    //        Steps = new();
    //        TextBoxes = new();
    //        CheckBoxes = new();
    //        SortedTextBoxes = new();
    //    }
    //}

    //public class TextBoxS
    //{
    //    public string Name
    //    {
    //        get;
    //        set;
    //    }

    //    public string Content
    //    {
    //        get;
    //        set;
    //    }

    //    public double X
    //    {
    //        get;
    //        set;
    //    }

    //    public double Y
    //    {
    //        get;
    //        set;
    //    }

    //    public bool IsEnabled
    //    {
    //        get;
    //        set;
    //    }
    //}

    //public class CheckBoxS
    //{
    //    public string Name
    //    {
    //        get;
    //        set;
    //    }

    //    public string Content
    //    {
    //        get;
    //        set;
    //    }

    //    public bool? IsChecked
    //    {
    //        get;
    //        set;
    //    }

    //    public double X
    //    {
    //        get;
    //        set;
    //    }

    //    public double Y
    //    {
    //        get;
    //        set;
    //    }

    //    public bool IsEnabled
    //    {
    //        get;
    //        set;
    //    }
    //}

}
