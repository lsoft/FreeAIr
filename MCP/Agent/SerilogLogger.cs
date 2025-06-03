using Serilog;
using System.Runtime.CompilerServices;

namespace Agent
{
    public static class SerilogLogger
    {
        public static string LogFolderPath
        {
            get;
            private set;
        }

        public static Serilog.Core.Logger Logger
        {
            get;
            private set;
        }

        public static void Init(
            string currentFolderPath,
            string logFolderName,
            string logFileName
            )
        {
            LogFolderPath = System.IO.Path.Combine(
                currentFolderPath,
                logFolderName
                );

            var logFilePath = System.IO.Path.Combine(
                LogFolderPath,
                logFileName
                );

            Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31,
                    fileSizeLimitBytes: 1 * 1024 * 1024,
                    shared: true,
                    //buffered: true,
                    outputTemplate: "{Timestamp:yyyy.MM.dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                    )
                .CreateLogger()
                ;
        }
    }

    public static class SerilogContext
    {
        public static void Fatal(
            Exception? exception,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Fatal(
                SerilogLogger.Logger,
                exception,
                message,
                memberName,
                sourceFilePath,
                sourceLineNumber
                );
        }

        public static void Fatal(
            this ILogger logger,
            Exception? exception,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            logger.Fatal(exception, "{ClassName}.{MemberName}({LineNumber}): {Message}", className, memberName, sourceLineNumber, message);
        }

        public static void Fatal(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Fatal(
                SerilogLogger.Logger,
                null,
                message,
                memberName,
                sourceFilePath,
                sourceLineNumber
                );
        }

        public static void Fatal(
            this ILogger logger,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            logger.Fatal("{ClassName}.{MemberName}({LineNumber}): {Message}", className, memberName, sourceLineNumber, message);
        }

        public static void Error(
            Exception? exception,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Error(
                SerilogLogger.Logger,
                exception,
                message,
                memberName,
                sourceFilePath,
                sourceLineNumber
                );
        }

        public static void Error(
            this ILogger logger,
            Exception? exception,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            logger.Error(exception, "{ClassName}.{MemberName}({LineNumber}): {Message}", className, memberName, sourceLineNumber, message);
        }

        public static void Error(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Error(
                SerilogLogger.Logger,
                message,
                memberName,
                sourceFilePath,
                sourceLineNumber
                );
        }

        public static void Error(
            this ILogger logger,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            logger.Error("{ClassName}.{MemberName}({LineNumber}): {Message}", className, memberName, sourceLineNumber, message);
        }

        public static void Warning(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Warning(
                SerilogLogger.Logger,
                message,
                memberName,
                sourceFilePath,
                sourceLineNumber
                );
        }

        public static void Warning(
            this ILogger logger,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            logger.Warning("{ClassName}.{MemberName}({LineNumber}): {Message}", className, memberName, sourceLineNumber, message);
        }

        public static void Information(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Information(
                SerilogLogger.Logger,
                message,
                memberName,
                sourceFilePath,
                sourceLineNumber
                );
        }

        public static void Information(
            this ILogger logger,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            logger.Information("{ClassName}.{MemberName}({LineNumber}): {Message}", className, memberName, sourceLineNumber, message);
        }
    }
}
