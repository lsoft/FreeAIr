using EnvDTE;
using EnvDTE80;
using FreeAIr.BLogic;
using Microsoft.VisualStudio;
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
