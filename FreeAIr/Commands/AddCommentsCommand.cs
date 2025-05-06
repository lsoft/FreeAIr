using EnvDTE;
using EnvDTE80;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Commands
{
    [Command(PackageIds.AddCommentsCommandId)]
    internal sealed class AddCommentsCommand : InvokeLLMCommand<AddCommentsCommand>
    {
        public AddCommentsCommand(
            )
        {
        }

        protected override ChatKindEnum GetChatKind() => ChatKindEnum.AddComments;
    }

}
