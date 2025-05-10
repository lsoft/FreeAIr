using EnvDTE;
using FreeAIr.BLogic;

namespace FreeAIr.Commands
{
    [Command(PackageIds.ExplainCommandId)]
    internal sealed class ExplainCodeCommand : InvokeLLMCommand<ExplainCodeCommand>
    {
        public ExplainCodeCommand(
            )
        {
        }

        protected override ChatKindEnum GetChatKind() => ChatKindEnum.ExplainCode;
    }
}
