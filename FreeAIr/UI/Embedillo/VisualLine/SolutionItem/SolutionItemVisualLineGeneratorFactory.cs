using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FreeAIr.UI.Embedillo.VisualLine.SolutionItem
{
    public sealed class SolutionItemVisualLineGeneratorFactory : IMentionVisualLineGeneratorFactory
    {
        public SolutionItemVisualLineGeneratorFactory(
            )
        {
        }

        public MentionVisualLineGenerator Create()
        {
            return new SolutionItemVisualLineGenerator(
                );
        }
    }

    public sealed class SolutionItemVisualLineGenerator : MentionVisualLineGenerator
    {
        public const char Anchor = '#';

        public SolutionItemVisualLineGenerator(
            ) : base(Anchor)
        {
        }

        public override IAnswerPart CreatePart(string partPayload)
        {
            return new SolutionItemAnswerPart(partPayload);
        }

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
                null
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
                var sp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    ToolTip = filePath
                };
                sp.Children.Add(
                    new TextBlock
                    {
                        Padding = new Thickness(2),
                        Text = fileName,
                        FontWeight = FontWeights.Bold,
                        ToolTip = filePath
                    }
                    );
                sp.Children.Add(
                    new TextBlock
                    {
                        Padding = new Thickness(2),
                        Text = " ✓ ",
                        Foreground = Brushes.Green,
                        Background = Brushes.LightGreen,
                        FontWeight = FontWeights.Bold,
                        ToolTip = filePath
                    }
                    );
                child = sp;
            }
            else
            {
                var sp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    ToolTip = filePath
                };
                sp.Children.Add(
                    new TextBlock
                    {
                        Padding = new Thickness(2),
                        Text = fileName,
                        FontWeight = FontWeights.Bold,
                        ToolTip = filePath
                    }
                    );
                sp.Children.Add(
                    new TextBlock
                    {
                        Padding = new Thickness(2),
                        Text = "<!>",
                        Foreground = Brushes.Red,
                        Background = Brushes.LightPink,
                        FontWeight = FontWeights.Bold,
                        ToolTip = filePath
                    }
                    );
                child = sp;
            }

            var border = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = fileExists ? Brushes.Green : Brushes.Red,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, -4),
                VerticalAlignment = VerticalAlignment.Center,
                Child = child,
                ToolTip = filePath
            };

            return border;
        }
    }
}
