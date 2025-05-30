using FreeAIr.BLogic;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FreeAIr.UI.Embedillo.VisualLine.Command
{
    public sealed class CommandVisualLineGeneratorFactory : IMentionVisualLineGeneratorFactory
    {
        public CommandVisualLineGeneratorFactory(
            )
        {
        }

        public MentionVisualLineGenerator Create(
            )
        {
            return new CommandVisualLineGenerator(
                );
        }
    }

    public sealed class CommandVisualLineGenerator : MentionVisualLineGenerator
    {
        public const char Anchor = '/';

        public CommandVisualLineGenerator(
            ) : base(Anchor)
        {
        }

        public override IAnswerPart CreatePart(string partPayload)
        {
            var kind = (ChatKindEnum)Enum.Parse(typeof(ChatKindEnum), partPayload);
            return new CommandAnswerPart(kind);
        }

        public override System.Threading.Tasks.Task<List<ISuggestion>> GetSuggestionsAsync()
        {
            var suggestions = GenerateSuggestions();

            return Task.FromResult(suggestions);
        }

        protected override UIElement CreateControl(string command)
        {
            command = command.TrimStart(Anchor);

            var suggestions = GenerateSuggestions();
            var suggestion = suggestions.FirstOrDefault(s => StringComparer.CurrentCultureIgnoreCase.Compare(s.PublicData, command) == 0);

            UIElement child;
            if (suggestion is not null)
            {
                var sp = new Grid
                {
                    Margin = new Thickness(5, 0, 5, 0),
                    ToolTip = suggestion.FullData,
                };
                sp.ColumnDefinitions.Add(new ColumnDefinition());
                sp.ColumnDefinitions.Add(new ColumnDefinition());

                var img0 = new CrispImage
                {
                    Moniker = new Microsoft.VisualStudio.Imaging.Interop.ImageMoniker
                    {
                        Guid = new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"),
                        Id = 2646
                    }
                };
                sp.Children.Add(img0);
                Grid.SetColumn(img0, 0);

                var tb1 = new TextBlock
                {
                    Padding = new Thickness(0),
                    Text = suggestion.PublicData,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    ToolTip = suggestion.FullData
                };
                sp.Children.Add(tb1);
                Grid.SetColumn(tb1, 1);

                child = sp;
            }
            else
            {
                var sp = new Grid
                {
                    Margin = new Thickness(5, 0, 5, 0),
                };
                sp.ColumnDefinitions.Add(new ColumnDefinition());
                sp.ColumnDefinitions.Add(new ColumnDefinition());

                var img0 = new CrispImage
                {
                    Moniker = new Microsoft.VisualStudio.Imaging.Interop.ImageMoniker
                    {
                        Guid = new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"),
                        Id = 2926
                    }
                };
                sp.Children.Add(img0);
                Grid.SetColumn(img0, 0);

                var tb1 = new TextBlock
                {
                    Padding = new Thickness(0),
                    Text = command,
                    VerticalAlignment = VerticalAlignment.Bottom,
                };
                sp.Children.Add(tb1);
                Grid.SetColumn(tb1, 1);

                child = sp;
            }

            var border = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = suggestion is not null ? Brushes.Green : Brushes.Red,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = child,
            };

            return border;
        }

        private static List<ISuggestion> GenerateSuggestions()
        {
            var suggestions = new List<ISuggestion>();

            suggestions.Add(
                new CommandSuggestion(
                    KnownMonikers.SQLServerObjectExplorer,
                    "ExplainCode",
                    "ExplainCode"
                    )
                );
            suggestions.Add(
                new CommandSuggestion(
                    KnownMonikers.CodeReviewWizard,
                    "AddXmlComments",
                    "AddXmlComments"
                    )
                );
            suggestions.Add(
                new CommandSuggestion(
                    KnownMonikers.TestGroup,
                    "GenerateUnitTests",
                    "GenerateUnitTests"
                    )
                );

            return suggestions;
        }
    }
}
