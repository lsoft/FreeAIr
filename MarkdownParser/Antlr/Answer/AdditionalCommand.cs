using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer
{
    /// <summary>
    /// Holds the extra per-part action buttons (e.g. "copy code") a host registers so
    /// <see cref="ParsedMarkdown.UpdateFlowDocument"/> can attach them next to matching
    /// <see cref="IPart"/>s while rendering.
    /// </summary>
    public sealed class AdditionalCommandContainer
    {
        private readonly List<AdditionalCommand> _additionalCommands;

        /// <summary>The registered commands.</summary>
        public IReadOnlyList<AdditionalCommand> AdditionalCommands => _additionalCommands;

        /// <summary>Creates an empty container with no commands registered.</summary>
        public AdditionalCommandContainer()
        {
            _additionalCommands = new();
        }

        /// <summary>Registers a command so it is offered for any part whose <see cref="IPart.Type"/> matches.</summary>
        public void AddAdditionalCommand(
            AdditionalCommand additionalCommand
            )
        {
            _additionalCommands.Add(additionalCommand);
        }

        /// <summary>Builds the bordered button strip for all registered commands that apply to <paramref name="part"/>, or null if none do.</summary>
        public InlineUIContainer? GetCommandControls(
            IPart part
            )
        {
            if (part is null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            var ourCommands = AdditionalCommands
                .Where(ac => ac.PartType.HasFlag(part.Type))
                .ToList()
                ;

            if (ourCommands.Count <= 0)
            {
                return null;
            }

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };
            
            foreach (var ourCommand in ourCommands)
            {
                UIElement? control;
                try
                {
                    control = ourCommand.CreateControl(
                        part
                        );
                }
                catch (Exception excp)
                {
                    //building one button's command parameter may fail on the part's own content;
                    //that button is then simply not offered, rather than taking the paragraph -
                    //and with it the whole chat window - down with it (issue #73)
                    WpfHelpers.CommandDiagnostics.Report(excp);
                    continue;
                }

                if (control is null)
                {
                    continue;
                }

                sp.Children.Add(control);
            }

            if (sp.Children.Count <= 0)
            {
                return null;
            }

            var border = new Border
            {
                Margin = new Thickness(2, 0, 6, 0),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                BorderBrush = Brushes.Green,
                Child = sp
            };

            var ic = new InlineUIContainer(border);
            ic.BaselineAlignment = BaselineAlignment.Center;
            return ic;
        }

    }

    /// <summary>
    /// One user-defined action button (e.g. "copy code", "insert into editor") that can be attached
    /// next to any rendered <see cref="IPart"/> whose type matches <see cref="PartType"/>.
    /// </summary>
    public /*sealed*/ class AdditionalCommand
    {
        private readonly IFontSizeProvider _fontSizeProvider;

        /// <summary>Which part types this command should be offered for.</summary>
        public PartTypeEnum PartType
        {
            get;
        }

        /// <summary>Button text.</summary>
        public string Title
        {
            get;
        }

        /// <summary>Button tooltip.</summary>
        public string ToolTip
        {
            get;
        }

        /// <summary>Command invoked on click; receives the part's context as its parameter, see <see cref="CreateControl"/>.</summary>
        public ICommand? ActionCommand
        {
            get;
        }

        /// <summary>Optional override for the button's foreground brush.</summary>
        public System.Windows.Media.Brush? Foreground
        {
            get;
        }

        /// <summary>Defines one action button offered next to parts of <paramref name="partType"/>.</summary>
        public AdditionalCommand(
            IFontSizeProvider fontSizeProvider,
            PartTypeEnum partType,
            string title,
            string toolTip,
            ICommand? actionCommand,
            System.Windows.Media.Brush? foreground
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (toolTip is null)
            {
                throw new ArgumentNullException(nameof(toolTip));
            }

            _fontSizeProvider = fontSizeProvider;
            PartType = partType;
            Title = title;
            ToolTip = toolTip;
            ActionCommand = actionCommand;
            Foreground = foreground;
        }

        /// <summary>Builds the WPF button for this command, wiring <see cref="ActionCommand"/> with the part's own command parameter.</summary>
        public virtual UIElement? CreateControl(
            IPart part
            )
        {
            var control = new Button
            {
                Margin = new Thickness(2, 0, 2, 0),
                FontSize = _fontSizeProvider.ContextButtonSize,
                FontFamily = new FontFamily("Cascadia Code"),
                ToolTip = ToolTip,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                Content = Title,
                Command = ActionCommand,
                CommandParameter = part.GetContextForAdditionalCommand(),
                Focusable = false
            };
            if (Foreground is not null)
            {
                control.Foreground = Foreground;
            }
            control.SetResourceReference(Button.StyleProperty, "TextBlockLikeButtonStyle");
            return control;
        }
    }
}
