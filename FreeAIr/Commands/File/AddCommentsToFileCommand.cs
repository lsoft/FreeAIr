using EnvDTE;
using FreeAIr.BLogic;

namespace FreeAIr.Commands.File
{
    [Command(PackageIds.AddCommentsToFileCommandId)]
    internal sealed class AddCommentsToFileCommand : InvokeLLMFileCommand<AddCommentsToFileCommand>
    {
        public AddCommentsToFileCommand(
            )
        {
        }

        protected override ChatKindEnum GetChatKind() => ChatKindEnum.AddXmlComments;
    }

}
