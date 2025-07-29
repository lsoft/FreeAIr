using FreeAIr.BLogic;

namespace FreeAIr.Helper
{
    public static class EnumHelper
    {
        public static string AsUIString(
            this ChatStatusEnum status
            )
        {
            switch (status)
            {
                case ChatStatusEnum.NotStarted:
                    return FreeAIr.Resources.Resources.Not_started;
                case ChatStatusEnum.WaitingForAnswer:
                    return FreeAIr.Resources.Resources.Waiting_for_answer;
                case ChatStatusEnum.ReadingAnswer:
                    return FreeAIr.Resources.Resources.Reading_answer;
                case ChatStatusEnum.Ready:
                    return FreeAIr.Resources.Resources.Ready;
                case ChatStatusEnum.Failed:
                    return FreeAIr.Resources.Resources.Failed;
                default:
                    return status.ToString();
            }
        }


    }
}
