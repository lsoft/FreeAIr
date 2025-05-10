using EnvDTE;
using FreeAIr.BLogic;

namespace FreeAIr.Commands.File
{
    [Command(PackageIds.ExplainFileCommandId)]
    internal sealed class ExplainFileCommand : InvokeLLMFileCommand<ExplainFileCommand>
    {
        public ExplainFileCommand(
            )
        {
        }

        protected override ChatKindEnum GetChatKind() => ChatKindEnum.ExplainCode;
    }
}
