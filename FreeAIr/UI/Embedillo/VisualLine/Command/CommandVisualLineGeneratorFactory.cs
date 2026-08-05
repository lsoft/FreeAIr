using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FreeAIr.UI.Embedillo.VisualLine.Command
{
    /// <summary>Creates <see cref="CommandVisualLineGenerator"/> instances so the prompt editor can render `/command` mentions inline.</summary>
    public sealed class CommandVisualLineGeneratorFactory : IMentionVisualLineGeneratorFactory
    {
        /// <summary>Creates the factory. No setup is required until <see cref="Create"/> is called.</summary>
        public CommandVisualLineGeneratorFactory(
            )
        {
        }

        /// <summary>Creates a new command visual-line generator for a prompt editor instance.</summary>
        public MentionVisualLineGenerator Create(
            )
        {
            return new CommandVisualLineGenerator(
                );
        }
    }

    /// <summary>
    /// Recognizes `/command` mentions typed in the prompt editor, offers matching support actions as
    /// autocomplete suggestions, and renders a highlighted chip in the input showing whether the typed
    /// command resolves to a known support action.
    /// </summary>
    public sealed class CommandVisualLineGenerator : MentionVisualLineGenerator
    {
        /// <summary>The character that introduces a command mention in the prompt text.</summary>
        public const char Anchor = '/';
        /// <summary>Support-action suggestions generated for the current autocomplete session.</summary>
        private List<CommandSuggestion>? _suggestions;

        /// <summary>Creates the generator, registering <see cref="Anchor"/> as the mention trigger character.</summary>
        public CommandVisualLineGenerator(
            ) : base(Anchor)
        {
        }

        /// <summary>Resolves a typed command to its support action's prompt template, with context variables applied.</summary>
        public override IParsedPart? CreatePart(string partPayload)
        {
            var suggestion = _suggestions?.FirstOrDefault(s => StringComparer.CurrentCultureIgnoreCase.Compare(s.PublicData, partPayload) == 0);
            if (suggestion is null)
            {
                return null;
            }

            var supportContext = SupportContext.WithPrompt(
                );

            var promptText = supportContext.ApplyVariablesToPrompt(
                suggestion.SupportAction.Prompt
                );

            return new CommandAnswerPart(promptText);
        }

        /// <summary>Loads the current list of support actions available as `/command` autocomplete suggestions.</summary>
        public override async System.Threading.Tasks.Task<List<ISuggestion>> GetSuggestionsAsync()
        {
            _suggestions = await GenerateSuggestionsAsync();
            return _suggestions.ConvertAll(s => (ISuggestion)s);
        }

        /// <summary>Builds the inline chip shown for a typed command, colored green when it matches a known support action and red otherwise.</summary>
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

                var img0 = new PseudoCrispImage
                {
                    Moniker = new Microsoft.VisualStudio.Imaging.Interop.ImageMoniker
                    {
                        Guid = new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"),
                        Id = 2646
                    },
                    Margin = new Thickness(0, 0, 5, 0),
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

                var img0 = new PseudoCrispImage
                {
                    Moniker = new Microsoft.VisualStudio.Imaging.Interop.ImageMoniker
                    {
                        Guid = new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"),
                        Id = 2926
                    },
                    Margin = new Thickness(0, 0, 5, 0),
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

        /// <summary>Loads support actions scoped to the prompt input control and converts each into a command suggestion.</summary>
        private static async Task<List<CommandSuggestion>> GenerateSuggestionsAsync()
        {
            var suggestions = new List<CommandSuggestion>();

            var actions = await FreeAIrOptions.DeserializeSupportActionsAsync(
                a => a.Scopes.Contains(SupportScopeEnum.EnterPromptControl)
                );
            foreach (var action in actions)
            {
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

        /// <summary>Formats the tooltip for a command chip, showing the support action's name and its underlying prompt template.</summary>
        private static string GenerateTooltip(SupportActionJson action)
        {
            return
$"""
{action.Name}
{Resources.Resources.Its_prompt_template}:
{action.Prompt}
""";

        }

        /// <summary>Converts a support action's display name into the CamelCase form used as its `/command` invocation text.</summary>
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
