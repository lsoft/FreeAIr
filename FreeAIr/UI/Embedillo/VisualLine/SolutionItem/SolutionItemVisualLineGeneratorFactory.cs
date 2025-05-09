﻿using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FreeAIr.UI.Embedillo.VisualLine.SolutionItem
{
    public sealed class SolutionItemVisualLineGeneratorFactory : IMentionVisualLineGeneratorFactory
    {
        public MentionVisualLineGenerator Create(ControlPositionManager positionManager)
        {
            return new SolutionItemVisualLineGenerator(
                positionManager
                );
        }
    }

    public sealed class SolutionItemVisualLineGenerator : MentionVisualLineGenerator
    {
        public const char Anchor = '#';

        public SolutionItemVisualLineGenerator(
            ControlPositionManager controlPositionManager
            ) : base(Anchor, controlPositionManager)
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

            List<(Community.VisualStudio.Toolkit.SolutionItem solutionItem, SelectedSpan Selection)> solutionItems = await solution.ProcessDownRecursivelyForAsync(
                SolutionItemType.PhysicalFile,
                null
                );

            var suggestions = solutionItems.ConvertAll(si =>
                (ISuggestion)new SolutionItemSuggestion(
                    si.solutionItem.FullPath,
                    si.solutionItem.FullPath.MakeRelativeAgainst(solutionFolderPath),
                    si.Selection
                    )
            );

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
