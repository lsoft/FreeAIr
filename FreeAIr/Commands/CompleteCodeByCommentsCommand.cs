using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Commands
{
    [Command(PackageIds.CompleteCodeByCommentsCommandId)]
    internal sealed class CompleteCodeByCommentsCommand : InvokeLLMCommand<CompleteCodeByCommentsCommand>
    {
        public CompleteCodeByCommentsCommand(
            )
        {
        }

        protected override ChatKindEnum GetChatKind() => ChatKindEnum.CompleteCodeAccordingComments;
    }

}
