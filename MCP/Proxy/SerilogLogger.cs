using Serilog;
using System.Runtime.CompilerServices;

namespace Proxy
{
    /// <summary>
    /// Owns the proxy process's single rolling-file Serilog logger - the proxy has no console or IDE
    /// output channel, so this file under `Log\` is the only place its behaviour can be observed.
    /// </summary>
    public static class SerilogLogger
    {
        /// <summary>Folder the rolling log files are written under, set by <see cref="Init"/>.</summary>
        public static string LogFolderPath
        {
            get;
            private set;
        }

        /// <summary>The proxy process's shared Serilog logger instance.</summary>
        public static Serilog.Core.Logger Logger
        {
            get;
            private set;
        }

        /// <summary>Creates the day-rolling file logger under <paramref name="currentFolderPath"/>/<paramref name="logFolderName"/>; must run before anything logs.</summary>
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
                //.WriteTo.Console()
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

    /// <summary>
    /// Log-level helpers that prefix every message with the calling class, member and line number
    /// (via <see cref="CallerMemberNameAttribute"/> et al.), so a proxy log line is as traceable as
    /// an IDE breakpoint without the caller spelling any of that out.
    /// </summary>
    public static class SerilogContext
    {
        /// <summary>Logs at Fatal level, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Fatal level on <paramref name="logger"/>, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Fatal level on the shared logger, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Fatal level on <paramref name="logger"/>, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Error level, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Error level on <paramref name="logger"/>, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Error level on the shared logger, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Error level on <paramref name="logger"/>, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Warning level, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Warning level on <paramref name="logger"/>, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Information level, prefixed with the caller's class/member/line.</summary>
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

        /// <summary>Logs at Information level on <paramref name="logger"/>, prefixed with the caller's class/member/line.</summary>
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
