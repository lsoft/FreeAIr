using FreeAIr.BLogic.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class QueryBuilder
    {

        public static string BuildQuery(
            AITaskKindEnum kind,
            string text
            )
        {
            switch (kind)
            {
                case AITaskKindEnum.ExplainCode:
                    return "Explain the following code:" + Environment.NewLine + text;
                case AITaskKindEnum.AddComments:
                    return "Add comments that match the following code:" + Environment.NewLine + text;
                case AITaskKindEnum.OptimizeCode:
                    return "Optimize the following code:" + Environment.NewLine + text;
                case AITaskKindEnum.CompleteCodeAccordingComments:
                    return "Optimize the following code:" + Environment.NewLine + text;
                case AITaskKindEnum.GenerateCode:
                    return "Generate code according to description:" + Environment.NewLine + text;
                default:
                    throw new InvalidOperationException($"Unknown kind {kind}");
            }
        }
    }
}
