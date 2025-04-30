using FreeAIr.BLogic.Tasks;
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
        public static string AsString(
            this AITaskStatusEnum status
            )
        {
            switch (status)
            {
                case AITaskStatusEnum.NotStarted:
                    return "Not started";
                case AITaskStatusEnum.WaitForAnswer:
                    return "Waiting for answer";
                case AITaskStatusEnum.ReadAnswer:
                    return "Reading answer";
                case AITaskStatusEnum.Completed:
                    return "Completed";
                case AITaskStatusEnum.Cancelled:
                    return "Cancelled";
                case AITaskStatusEnum.Failed:
                    return "Failed";
                default:
                    return status.ToString();
            }
        }


        public static string AsString(
            this AITaskKindEnum taskKind
            )
        {
            switch (taskKind)
            {
                case AITaskKindEnum.ExplainCode:
                    return "Explain code";
                case AITaskKindEnum.AddComments:
                    return "Add comments";
                case AITaskKindEnum.OptimizeCode:
                    return "Opimize code";
                case AITaskKindEnum.CompleteCodeAccordingComments:
                    return "Complete the code according to the comments";
                case AITaskKindEnum.GenerateCode:
                    return "Generate code";
                default:
                    return taskKind.ToString();
            }
        }
    }
}
