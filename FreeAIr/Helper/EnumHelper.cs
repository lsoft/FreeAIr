using EnvDTE;
using FreeAIr.BLogic;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class EnumHelper
    {
        public static string AsPromptString(
            this ChatKindEnum kind
            )
        {
            switch (kind)
            {
                case ChatKindEnum.ExplainCode:
                    return "Explain the following code:";
                case ChatKindEnum.AddComments:
                    return "Add comments that match the following code:";
                case ChatKindEnum.OptimizeCode:
                    return "Optimize the following code:";
                case ChatKindEnum.CompleteCodeAccordingComments:
                    return "Complete the following code according its comments:";
                case ChatKindEnum.GenerateCode:
                    return "Generate code according to description:";
                default:
                    throw new InvalidOperationException($"Unknown kind {kind}");
            }
        }

        public static string AsShortString(
            this ChatKindEnum taskKind
            )
        {
            switch (taskKind)
            {
                case ChatKindEnum.ExplainCode:
                    return "Explain code";
                case ChatKindEnum.AddComments:
                    return "Add comments";
                case ChatKindEnum.OptimizeCode:
                    return "Opimize code";
                case ChatKindEnum.CompleteCodeAccordingComments:
                    return "Complete the code according to the comments";
                case ChatKindEnum.GenerateCode:
                    return "Generate code";
                default:
                    return taskKind.ToString();
            }
        }

        public static string AsString(
            this ChatPromptStatusEnum status
            )
        {
            switch (status)
            {
                case ChatPromptStatusEnum.NotStarted:
                    return "Not started";
                case ChatPromptStatusEnum.WaitForAnswer:
                    return "Waiting for answer";
                case ChatPromptStatusEnum.ReadAnswer:
                    return "Reading answer";
                case ChatPromptStatusEnum.Completed:
                    return "Completed";
                case ChatPromptStatusEnum.Cancelled:
                    return "Cancelled";
                case ChatPromptStatusEnum.Failed:
                    return "Failed";
                default:
                    return status.ToString();
            }
        }


    }
}
