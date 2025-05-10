using EnvDTE;
using FreeAIr.BLogic;

namespace FreeAIr.Commands
{
    [Command(PackageIds.OptimizeCommandId)]
    internal sealed class OptimizeCodeCommand : InvokeLLMCommand<OptimizeCodeCommand>
    {
        public OptimizeCodeCommand(
            )
        {
        }

        protected override ChatKindEnum GetChatKind() => ChatKindEnum.OptimizeCode;

    }

}
