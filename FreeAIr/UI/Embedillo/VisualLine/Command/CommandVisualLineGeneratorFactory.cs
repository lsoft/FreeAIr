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
        public MentionVisualLineGenerator Create(ControlPositionManager positionManager)
        {
            return new CommandVisualLineGenerator(
                positionManager
                );
        }
    }

    public sealed class CommandVisualLineGenerator : MentionVisualLineGenerator
    {
        public const char Anchor = '/';

        public CommandVisualLineGenerator(
            ControlPositionManager controlPositionManager
            ) : base(Anchor, controlPositionManager)
        {
        }

        public override IAnswerPart CreatePart(string partPayload)
        {
            var kind = (ChatKindEnum)Enum.Parse(typeof(ChatKindEnum), partPayload);
            return new CommandAnswerPart(kind);
        }

        public override System.Threading.Tasks.Task<List<Suggestion>> GetSuggestionsAsync()
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
                var sp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    ToolTip = suggestion.FullData
                };

                sp.Children.Add(
                    new CrispImage
                    {
                        Moniker = new Microsoft.VisualStudio.Imaging.Interop.ImageMoniker
                        {
                            Guid = new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"),
                            Id = 2646
                        }
                    }
                    );
                sp.Children.Add(
                    new TextBlock
                    {
                        Padding = new Thickness(2),
                        Text = suggestion.PublicData,
                        FontWeight = FontWeights.Bold,
                        ToolTip = suggestion.FullData
                    }
                    );
                child = sp;
            }
            else
            {
                var sp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    ToolTip = "Invalid command"
                };
                sp.Children.Add(
                    new CrispImage
                    {
                        Moniker = new Microsoft.VisualStudio.Imaging.Interop.ImageMoniker
                        {
                            Guid = new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"),
                            Id = 2926
                        }
                    }
                    );
                sp.Children.Add(
                    new TextBlock
                    {
                        Padding = new Thickness(2),
                        Text = command,
                        FontWeight = FontWeights.Bold,
                        ToolTip = "Invalid command"
                    }
                    );
                child = sp;
            }

            var border = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = suggestion is not null ? Brushes.Green : Brushes.Red,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, -4),
                VerticalAlignment = VerticalAlignment.Center,
                Child = child,
                //ToolTip = filePath
            };

            return border;
        }

        private static List<Suggestion> GenerateSuggestions()
        {
            var suggestions = new List<Suggestion>();

            suggestions.Add(
                new Suggestion(
                    "ExplainCode",
                    "ExplainCode"
                    )
                );
            suggestions.Add(
                new Suggestion(
                    "AddXmlComments",
                    "AddXmlComments"
                    )
                );
            suggestions.Add(
                new Suggestion(
                    "GenerateUnitTests",
                    "GenerateUnitTests"
                    )
                );

            return suggestions;
        }
    }
}
