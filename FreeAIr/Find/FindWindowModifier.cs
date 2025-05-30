using EnvDTE;
using EnvDTE80;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ContextMenuButton;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

                        var naturalSearchButton = new MenuButton
                        {
                            Margin = findAllButton.Margin,
                            VerticalAlignment = findAllButton.VerticalAlignment,
                            VerticalContentAlignment = findAllButton.VerticalContentAlignment,
                            Height = findAllButton.ActualHeight,
                            ToolTip = "Find using natural language in every file of current solution",
                            ContextClickItemCommand = new AsyncRelayCommand(
                                async a =>
                                {
                                    if (a is not ContextMenuItem cmi)
                                    {
                                        return;
                                    }

                                    var fileTypesFilter = fileTypesFilterTextBox.Text;
                                    var filesTypeFilters = new FileTypesFilter(
                                        fileTypesFilter
                                            .Split(';')
                                            .ConvertAll(f => new FileTypeFilter(f))
                                        );

                                    var scope = (NaturalSearchScopeEnum)cmi.Tag;

                                    //закрываем окно поиска
                                    CloseFindWindow();

                                    var pane = await NaturalLanguageResultsToolWindow.ShowAsync();
                                    var toolWindow = pane.Content as NaturalLanguageResultsToolWindowControl;
                                    var viewModel = toolWindow.DataContext as NaturalLanguageResultsViewModel;
                                    viewModel.SetNewChatAsync(scope, subjectToSearchTextBox.Text, filesTypeFilters)
                                        .FileAndForget(nameof(NaturalLanguageResultsViewModel.SetNewChatAsync));
                                })
                        };
                        naturalSearchButton.SetContent(
                            new TextBlock
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Text = "Find using natural language"
                            }
                            );
                        naturalSearchButton.SetButtonStyle(findAllButton.Style);
                        naturalSearchButton.ContextItems.Add(
                            new ContextMenuItem
                            {
                                Header = "Whole solution",
                                Tag = NaturalSearchScopeEnum.WholeSolution
                            }
                            );
                        naturalSearchButton.ContextItems.Add(
                            new ContextMenuItem
                            {
                                Header = "Current project",
                                Tag = NaturalSearchScopeEnum.CurrentProject
                            }
                            );

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

    public sealed class FileTypesFilter
    {
        public IReadOnlyList<FileTypeFilter> IncludeFilters
        {
            get;
        }

        public IReadOnlyList<FileTypeFilter> ExcludeFilters
        {
            get;
        }

        public FileTypesFilter(
            IReadOnlyList<FileTypeFilter> filters
            )
        {
            if (filters is null)
            {
                throw new ArgumentNullException(nameof(filters));
            }

            IncludeFilters = filters.FindAll(f => !f.Exclude);
            ExcludeFilters = filters.FindAll(f => f.Exclude);
        }

        public bool Match(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (IncludeFilters.Count == 0 && ExcludeFilters.Count == 0)
            {
                return true;
            }

            if (ExcludeFilters.Count > 0 && ExcludeFilters.Any(f => f.Match(filePath)))
            {
                return false;
            }
            if (IncludeFilters.Count == 0 || IncludeFilters.Any(f => f.Match(filePath)))
            {
                return true;
            }

            return false;
        }
    }

    public sealed class FileTypeFilter
    {
        public bool Exclude
        {
            get;
        }

        public string WildcardFilter
        {
            get;
        }

        public Regex RegexFilter
        {
            get;
        }

        public FileTypeFilter(
            string filter
            )
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            filter = filter.Trim();
            if (filter.StartsWith("!"))
            {
                Exclude = true;
                filter = filter.Substring(1);
            }

            WildcardFilter = filter;
            RegexFilter = new Regex(
                filter.WildCardToRegular(),
                RegexOptions.IgnoreCase | RegexOptions.Singleline //not a compiled, this is NOT A STATIC regex!
                );
        }

        public bool Match(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return RegexFilter.IsMatch(filePath);
        }
    }

}
