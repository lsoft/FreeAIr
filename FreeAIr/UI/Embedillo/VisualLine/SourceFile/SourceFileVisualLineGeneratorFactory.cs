using FreeAIr.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FreeAIr.UI.Embedillo.VisualLine.SourceFile
{
    public sealed class SourceFileVisualLineGeneratorFactory : IMentionVisualLineGeneratorFactory
    {
        public MentionVisualLineGenerator Create(ControlPositionManager positionManager)
        {
            return new SourceFileVisualLineGenerator(
                positionManager
                );
        }
    }

    public sealed class SourceFileVisualLineGenerator : MentionVisualLineGenerator
    {
        public const char Anchor = '#';

        public SourceFileVisualLineGenerator(
            ControlPositionManager controlPositionManager
            ) : base(Anchor, controlPositionManager)
        {
        }

        public override async System.Threading.Tasks.Task<List<Suggestion>> GetSuggestionsAsync()
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution == null)
            {
                return new List<Suggestion>();
            }

            var solutionFolderPath = new FileInfo(solution.FullPath).Directory.FullName;

            var files = await solution.ProcessDownRecursivelyForAsync(SolutionItemType.PhysicalFile, null);

            var suggestions = files.ConvertAll(f =>
                new Suggestion(
                f.FullPath,
                f.FullPath.MakeRelativeAgainst(solutionFolderPath)
                )
            );

            return suggestions;
        }

        protected override UIElement CreateControl(string filePath)
        {
            filePath = filePath.TrimStart(Anchor);

            var fileExists = System.IO.File.Exists(filePath);

            var fileName = fileExists
                ? new System.IO.FileInfo(filePath).Name
                : filePath
                ;

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
