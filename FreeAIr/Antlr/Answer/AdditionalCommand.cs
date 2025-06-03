using FreeAIr.Antlr.Answer.Parts;
using System.Collections.Generic;
using System.Windows.Input;

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
