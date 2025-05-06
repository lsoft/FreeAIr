using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

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
