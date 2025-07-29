using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MarkdownParser.Antlr.Answer
{
    public sealed class AdditionalCommandContainer
    {
        private readonly List<AdditionalCommand> _additionalCommands;

        public IReadOnlyList<AdditionalCommand> AdditionalCommands => _additionalCommands;

        public AdditionalCommandContainer()
        {
            _additionalCommands = new();
        }

        public void AddAdditionalCommand(
            AdditionalCommand additionalCommand
            )
        {
            _additionalCommands.Add(additionalCommand);
        }

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
                var control = ourCommand.CreateControl(
                    part
                    );
                if (control is null)
                {
                    continue;
                }

                sp.Children.Add(control);
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

    public /*sealed*/ class AdditionalCommand
    {
        private readonly IFontSizeProvider _fontSizeProvider;

        public PartTypeEnum PartType
        {
            get;
        }

        public string Title
        {
            get;
        }

        public string ToolTip
        {
            get;
        }

        public ICommand? ActionCommand
        {
            get;
        }

        public System.Windows.Media.Brush? Foreground
        {
            get;
        }

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
