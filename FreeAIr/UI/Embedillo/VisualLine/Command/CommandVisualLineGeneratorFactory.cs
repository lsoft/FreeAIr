using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private List<CommandSuggestion>? _suggestions;

        public CommandVisualLineGenerator(
            ) : base(Anchor)
        {
        }

        public override IParsedPart CreatePart(string partPayload)
        {
            var suggestion = _suggestions?.First(s => StringComparer.CurrentCultureIgnoreCase.Compare(s.PublicData, partPayload) == 0);

            var supportContext = SupportContext.WithPrompt(
                );

            var promptText = supportContext.ApplyVariablesToPrompt(
                suggestion.SupportAction.Prompt
                );

            return new CommandAnswerPart(promptText);
        }

        public override async System.Threading.Tasks.Task<List<ISuggestion>> GetSuggestionsAsync()
        {
            _suggestions = await GenerateSuggestionsAsync();
            return _suggestions.ConvertAll(s => (ISuggestion)s);
        }

        protected override UIElement CreateControl(string command)
        {
            command = command.TrimStart(Anchor);

            var suggestion = _suggestions?.FirstOrDefault(s => StringComparer.CurrentCultureIgnoreCase.Compare(s.PublicData, command) == 0);

            UIElement child;
            if (suggestion is not null)
            {
                var sp = new Grid
                {
                    Margin = new Thickness(5, 0, 5, 0),
                    ToolTip = GenerateTooltip(suggestion.SupportAction),
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
                    Text = suggestion.SupportAction.Name,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    ToolTip = GenerateTooltip(suggestion.SupportAction)
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

        private static async Task<List<CommandSuggestion>> GenerateSuggestionsAsync()
        {
            var suggestions = new List<CommandSuggestion>();

            var support = await FreeAIrOptions.DeserializeSupportCollectionAsync();
            foreach (var action in support.Actions)
            {
                if (!action.Scopes.Contains(SupportScopeEnum.EnterPromptControl))
                {
                    continue;
                }

                var transformedName = TransformSupportActionName(action.Name);

                suggestions.Add(
                    new CommandSuggestion(
                        KnownMonikersHelper.GetMoniker(action.KnownMoniker),
                        transformedName,
                        transformedName,
                        action
                        )
                    );
            }

            return suggestions;
        }

        private static string GenerateTooltip(SupportActionJson action)
        {
            return
$"""
{action.Name}
Its prompt template:
{action.Prompt}
""";

        }

        private static string TransformSupportActionName(string name)
        {
            var sb = new StringBuilder();

            foreach (var part in name.Split(' ', '\t'))
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                sb.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                {
                    sb.Append(part.Substring(1));
                }
            }

            return sb.ToString();
        }
    }
}
