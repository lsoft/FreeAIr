using FreeAIr.BLogic;

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
                case ChatKindEnum.AddXmlComments:
                case ChatKindEnum.OptimizeCode:
                case ChatKindEnum.CompleteCodeAccordingComments:
                case ChatKindEnum.GenerateCommitMessage:
                case ChatKindEnum.FixBuildError:
                case ChatKindEnum.NaturalLanguageSearch:
                    return FreeAIr.Resources.Resources.ResourceManager.GetString(
                        nameof(ChatKindEnum) + "_" + kind.ToString(),
                        ResponsePage.GetAnswerCulture()
                        );
                case ChatKindEnum.Discussion:
                    return string.Empty;
                case ChatKindEnum.SuggestWholeLine:
                    {
                        var prompt = "ChatKindEnum_SuggestWholeLine".GetLocalizedResourceByName();
                        var anchor = "ChatKindEnum_SuggestWholeLine_Anchor".GetLocalizedResourceByName();
                        return string.Format(prompt, anchor);
                    }
                case ChatKindEnum.GenerateUnitTests:
                    {
                        var prompt = "ChatKindEnum_GenerateUnitTests".GetLocalizedResourceByName();
                        var anchor = ResponsePage.Instance.PreferredUnitTestFramework;
                        return string.Format(prompt, anchor);
                    }
                default:
                    throw new InvalidOperationException($"Unknown kind {kind}");
            }
        }

        public static string AsUIString(
            this ChatKindEnum taskKind
            )
        {
            switch (taskKind)
            {
                case ChatKindEnum.ExplainCode:
                    return "Explain code";
                case ChatKindEnum.AddXmlComments:
                    return "Add XML comments";
                case ChatKindEnum.OptimizeCode:
                    return "Optimize code";
                case ChatKindEnum.CompleteCodeAccordingComments:
                    return "Complete the code according to the comments";
                case ChatKindEnum.Discussion:
                    return "Discussion";
                case ChatKindEnum.GenerateCommitMessage:
                    return "Commit message";
                case ChatKindEnum.SuggestWholeLine:
                    return "Suggest whole line";
                case ChatKindEnum.FixBuildError:
                    return "Fix build error";
                case ChatKindEnum.NaturalLanguageSearch:
                    return "Find using natural language";
                default:
                    return taskKind.ToString();
            }
        }

        public static string AsUIString(
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
                    return "Ready";
                case ChatStatusEnum.Failed:
                    return "Failed";
                default:
                    return status.ToString();
            }
        }


    }
}
