using FreeAIr.Antlr.Answer.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FreeAIr.Antlr.Answer
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
                var tb = new TextBlock
                {
                    Margin = new Thickness(2, 0, 2, 0),
                    FontSize = 12,
                    FontFamily = new FontFamily("Cascadia Code"),
                    ToolTip = ourCommand.ToolTip,
                    FontWeight = FontWeights.Bold,
                    Foreground = ourCommand.Foreground,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand,
                    Text = ourCommand.Title
                };
                tb.InputBindings.Add(
                    new MouseBinding(
                        ourCommand.ActionCommand,
                        new MouseGesture(MouseAction.LeftClick)
                        )
                    {
                        CommandParameter = part.GetContextForAdditionalCommand()
                    }
                    );
                sp.Children.Add(tb);
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

    public sealed class AdditionalCommand
    {
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
        public ICommand ActionCommand
        {
            get;
        }

        public System.Windows.Media.Brush Foreground
        {
            get;
        }

        public AdditionalCommand(
            PartTypeEnum partType,
            string title,
            string toolTip,
            ICommand actionCommand,
            System.Windows.Media.Brush foreground
            )
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (toolTip is null)
            {
                throw new ArgumentNullException(nameof(toolTip));
            }

            if (actionCommand is null)
            {
                throw new ArgumentNullException(nameof(actionCommand));
            }

            if (foreground is null)
            {
                throw new ArgumentNullException(nameof(foreground));
            }

            PartType = partType;
            Title = title;
            ToolTip = toolTip;
            ActionCommand = actionCommand;
            Foreground = foreground;
        }
    }
}
