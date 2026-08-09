using Microsoft.VisualStudio.Shell;
using System;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Writes to Visual Studio's Activity Log under the "FreeAIr" source, the place to look when a
    /// feature silently did nothing — most background operations report failures here rather than
    /// throwing into the UI.
    /// </summary>
    public static class ActivityLogHelper
    {
        /// <summary>
        /// Logs an informational message under the "FreeAIr" source.
        /// </summary>
        public static void ActivityLogInformation(
            string message
            )
        {
            ActivityLog.LogInformation(
                "FreeAIr",
                message
                );
        }

        /// <summary>
        /// Logs a warning message under the "FreeAIr" source.
        /// </summary>
        public static void ActivityLogWarning(
            string message
            )
        {
            ActivityLog.LogWarning(
                "FreeAIr",
                message
                );
        }

        /// <summary>
        /// Logs an exception's type, message and stack trace as errors, and recurses into
        /// <see cref="Exception.InnerException"/>, indenting each nested level so the whole chain is
        /// readable in the Activity Log.
        /// </summary>
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
