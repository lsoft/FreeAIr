namespace FreeAIr.Helper
{
    public static class ActivityLogHelper
    {
        public static void ActivityLogInformation(
            string message
            )
        {
            ActivityLog.LogInformation(
                "FreeAIr",
                message
                );
        }

        public static void ActivityLogWarning(
            string message
            )
        {
            ActivityLog.LogWarning(
                "FreeAIr",
                message
                );
        }

        public static void ActivityLogException(
            this Exception excp,
            string message = "",
            int shift = 0
            )
        {
            if (!string.IsNullOrEmpty(message))
            {
                ActivityLog.LogError(
                    "FreeAIr",
                    message
                    );
            }

            var prefix = string.Empty;
            if (shift > 0)
            {
                prefix = new string(' ', shift);
            }

            ActivityLog.LogError(
                "FreeAIr",
                prefix + $"({excp.GetType().Name})" + excp.Message
                );
            ActivityLog.LogError(
                "FreeAIr",
                prefix + excp.StackTrace
                );

            if (excp.InnerException is not null)
            {
                ActivityLogException(
                    excp.InnerException,
                    string.Empty,
                    shift + 4
                    );
            }
        }
    }
}
