using EnvDTE;
using FreeAIr.BLogic;

namespace FreeAIr.Commands
{
    [Command(PackageIds.AddXmlCommentsCommandId)]
    internal sealed class AddXmlCommentsCommand : InvokeLLMCommand<AddXmlCommentsCommand>
    {
        public AddXmlCommentsCommand(
            )
        {
        }

        protected override ChatKindEnum GetChatKind() => ChatKindEnum.AddXmlComments;
    }
}
