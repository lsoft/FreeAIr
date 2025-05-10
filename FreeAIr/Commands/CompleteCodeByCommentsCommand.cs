using EnvDTE;
using FreeAIr.BLogic;

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
