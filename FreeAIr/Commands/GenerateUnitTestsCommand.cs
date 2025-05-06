using EnvDTE;
using FreeAIr.BLogic;

namespace FreeAIr.Commands
{
    [Command(PackageIds.GenerateUnitTestsCommandId)]
    internal sealed class GenerateUnitTestsCommand : InvokeLLMCommand<GenerateUnitTestsCommand>
    {
        public GenerateUnitTestsCommand(
            )
        {
        }

        protected override ChatKindEnum GetChatKind() => ChatKindEnum.GenerateUnitTests;

    }
}
