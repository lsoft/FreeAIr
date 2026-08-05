using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FreeAIr.UI.Embedillo.VisualLine.SolutionItem
{
    /// <summary>Creates <see cref="SolutionItemVisualLineGenerator"/> instances so the prompt editor can render `#file` mentions inline.</summary>
    public sealed class SolutionItemVisualLineGeneratorFactory : IMentionVisualLineGeneratorFactory
    {
        public SolutionItemVisualLineGeneratorFactory(
            )
        {
        }

        /// <summary>Creates a new solution-item visual-line generator for a prompt editor instance.</summary>
        public MentionVisualLineGenerator Create()
        {
            return new SolutionItemVisualLineGenerator(
                );
        }
    }

    /// <summary>
    /// Recognizes `#file` mentions typed in the prompt editor, offers solution files/projects as
    /// autocomplete suggestions, and renders a chip showing whether the referenced path still exists
    /// on disk.
    /// </summary>
    public sealed class SolutionItemVisualLineGenerator : MentionVisualLineGenerator
    {
        /// <summary>The character that introduces a solution-item mention in the prompt text.</summary>
        public const char Anchor = '#';

        /// <summary>Creates the generator, registering <see cref="Anchor"/> as the mention trigger character.</summary>
        public SolutionItemVisualLineGenerator(
            ) : base(Anchor)
        {
        }

        /// <summary>Wraps a mentioned file path (with optional selection) as a resolvable answer part.</summary>
        public override IParsedPart? CreatePart(string partPayload)
        {
            return new SolutionItemAnswerPart(partPayload);
        }

        /// <summary>Enumerates the current solution's projects and files as mention autocomplete suggestions.</summary>
        public override async System.Threading.Tasks.Task<List<ISuggestion>> GetSuggestionsAsync()
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution == null)
            {
                return new List<ISuggestion>();
            }

            var solutionFolderPath = new FileInfo(solution.FullPath).Directory.FullName;

            var solutionItems = await solution.ProcessDownRecursivelyForAsync(
                [SolutionItemType.Solution, SolutionItemType.Project, SolutionItemType.PhysicalFile],
                null,
                true,
                CancellationToken.None
                );

            var suggestions = solutionItems.ConvertAll(si =>
            {
                ImageMoniker im = KnownMonikers.QuestionMark;
                switch (si.SolutionItem.Type)
                {
                    case SolutionItemType.Solution:
                        im = KnownMonikers.Solution;
                        break;
                    case SolutionItemType.Project:
                        im = KnownMonikers.Application;
                        break;
                    case SolutionItemType.PhysicalFile:
                        if (si.Selection is null)
                        {
                            im = KnownMonikers.VBSourceFile;
                        }
                        else
                        {
                            im = KnownMonikers.FormatSelection;
                        }
                        break;
                }

                return
                    (ISuggestion) new SolutionItemSuggestion(
                        im,
                        si.SolutionItem.FullPath,
                        si.SolutionItem.FullPath.MakeRelativeAgainst(solutionFolderPath),
                        si.Selection
                        );
            });

            return suggestions;
        }

        /// <summary>Builds the inline chip shown for a `#file` mention, colored green when the file exists on disk and red otherwise.</summary>
        protected override UIElement CreateControl(string combinedFilePath)
        {
            var selectedIdentifier = SelectedIdentifier.Parse(combinedFilePath);

            var filePath = selectedIdentifier.FilePath.TrimStart(Anchor);

            var fileExists = System.IO.File.Exists(filePath);

            var fileName = fileExists
                ? new System.IO.FileInfo(filePath).Name
                : filePath
                ;
            
            fileName += selectedIdentifier.Selection?.ToString();

            UIElement child;
            if (fileExists)
            {
                var sp = new Grid
                {
                    Margin = new Thickness(5, 0, 0, 0),
                    ToolTip = filePath
                };
                sp.ColumnDefinitions.Add(new ColumnDefinition());
                sp.ColumnDefinitions.Add(new ColumnDefinition());

                var tb1 = new TextBlock
                {
                    Padding = new Thickness(0),
                    Text = fileName,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    FontFamily = new FontFamily("Cascadia Code"),
                    ToolTip = filePath
                };
                sp.Children.Add(tb1);
                Grid.SetColumn(tb1, 0);

                var img0 = new PseudoCrispImage
                {
                    Moniker = KnownMonikers.AddTextFile,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                sp.Children.Add(img0);
                Grid.SetColumn(img0, 1);

                child = sp;
            }
            else
            {
                var sp = new Grid
                {
                    Margin = new Thickness(5, 0, 0, 0),
                };
                sp.ColumnDefinitions.Add(new ColumnDefinition());
                sp.ColumnDefinitions.Add(new ColumnDefinition());

                var tb1 = new TextBlock
                {
                    Padding = new Thickness(0),
                    Text = fileName,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    FontFamily = new FontFamily("Cascadia Code"),
                    ToolTip = filePath
                };
                sp.Children.Add(tb1);
                Grid.SetColumn(tb1, 0);

                var img0 = new PseudoCrispImage
                {
                    Moniker = KnownMonikers.ExclamationPoint,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                sp.Children.Add(img0);
                Grid.SetColumn(img0, 1);

                child = sp;
            }

            var border = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = fileExists ? Brushes.Green : Brushes.Red,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = child,
                ToolTip = filePath
            };

            return border;
        }
    }
}
