using FreeAIr.BLogic;
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
                case ChatKindEnum.AddComments:
                case ChatKindEnum.OptimizeCode:
                case ChatKindEnum.CompleteCodeAccordingComments:
                    return FreeAIr.Resources.Resources.ResourceManager.GetString(
                        nameof(ChatKindEnum) + "_" + kind.ToString(),
                        ResponsePage.GetAnswerCulture()
                        );
                case ChatKindEnum.Discussion:
                    return string.Empty;
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
                    return "Optimize code";
                case ChatKindEnum.CompleteCodeAccordingComments:
                    return "Complete the code according to the comments";
                case ChatKindEnum.Discussion:
                    return "Discussion";
                default:
                    return taskKind.ToString();
            }
        }

        public static string AsString(
            this ChatStatusEnum status
            )
        {
            switch (status)
            {
                case ChatStatusEnum.NotStarted:
                    return "Not started";
                case ChatStatusEnum.WaitingForAnswer:
                    return "Waiting for answer";
                case ChatStatusEnum.ReadingAnswer:
                    return "Reading answer";
                case ChatStatusEnum.Ready:
                    return "Completed";
                case ChatStatusEnum.Cancelled:
                    return "Cancelled";
                case ChatStatusEnum.Failed:
                    return "Failed";
                default:
                    return status.ToString();
            }
        }


    }
}
