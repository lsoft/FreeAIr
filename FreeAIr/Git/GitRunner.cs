using FreeAIr.Helper;
using RunProcessAsTask;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Git
{
    /// <summary>
    /// Thin wrapper around the `git.exe` command line, run against the repository resolved by
    /// <see cref="GitRepositoryProvider"/>. Everything above (diff collection, commit message
    /// building) that needs to run an actual git command goes through here.
    /// </summary>
    public sealed class GitRunner
    {
        /// <summary>
        /// Runs `git diff HEAD` for the whole working tree.
        /// </summary>
        public static async Task<ProcessResults> DiffAsync(
            CancellationToken cancellationToken = default
            )
        {
            var result = await ProcessHelper.RunSilentlyAsync(
                await GitRepositoryProvider.GetRepositoryFolderAsync(),
                "git.exe",
                @"diff HEAD",
                cancellationToken
                );
            if (result.ExitCode != 0)
            {
                throw new GitProcessException(result, "diff fails");
            }
            return result;
        }

        /// <summary>
        /// Diffs a single untracked file against `/dev/null`, so its full content shows up as an
        /// addition in the collected diff.
        /// </summary>
        public static async Task<ProcessResults> DiffNewFileAsync(
            string filePath,
            CancellationToken cancellationToken = default
            )
        {
            var result = await ProcessHelper.RunSilentlyAsync(
                await GitRepositoryProvider.GetRepositoryFolderAsync(),
                "git.exe",
                $"diff --no-index /dev/null {filePath}",
                cancellationToken
                );
            ThrowIfFails(result, $"diff {filePath} fails");
            return result;
        }

        /// <summary>
        /// Turns a failed git invocation into a <see cref="GitProcessException"/>, treating exit
        /// code 1 as success since `git diff` uses it to mean "differences found".
        /// </summary>
        private static void ThrowIfFails(
            ProcessResults result,
            string errorMessage
            )
        {
            if ((result.ExitCode != 0 && result.ExitCode != 1)
                || result.StandardError.Length > 0
                )
            {
                throw new GitProcessException(result, errorMessage);
            }
        }

        /// <summary>
        /// Lists the untracked files of the repository (`git ls-files --others --exclude-standard`),
        /// respecting `.gitignore`.
        /// </summary>
        public static async Task<ProcessResults> ListFilesAsync(
            CancellationToken cancellationToken = default
            )
        {
            var result = await ProcessHelper.RunSilentlyAsync(
                await GitRepositoryProvider.GetRepositoryFolderAsync(),
                "git.exe",
                @"ls-files --others --exclude-standard",
                cancellationToken
                );
            ThrowIfFails(result, $"ls-files fails");
            return result;
        }

        /// <summary>
        /// Stages every change (`git add -A .`) and commits it with the given message.
        /// </summary>
        public static async Task CommitAsync(
            string message,
            CancellationToken cancellationToken = default
            )
        {
            var result = await ProcessHelper.RunSilentlyAsync(
                await GitRepositoryProvider.GetRepositoryFolderAsync(),
                "git.exe",
                $@"add -A .",
                cancellationToken
                );
            ThrowIfFails(result, $"add fails");

            result = await ProcessHelper.RunSilentlyAsync(
                await GitRepositoryProvider.GetRepositoryFolderAsync(),
                "git.exe",
                $@"commit -m ""{message}""",
                cancellationToken
                );
            ThrowIfFails(result, $"commit fails");
        }
    }

    /// <summary>
    /// Thrown when a `git.exe` invocation run by <see cref="GitRunner"/> fails, carrying the raw
    /// <see cref="ProcessResults"/> so the caller can inspect exit code and stderr.
    /// </summary>
    public sealed class GitProcessException : Exception
    {
        /// <summary>
        /// The result of the failed git process invocation.
        /// </summary>
        public ProcessResults ProcessResults
        {
            get;
        }

        /// <summary>Wraps a failed process result with no additional message.</summary>
        public GitProcessException(
            ProcessResults processResults
            )
        {
            ProcessResults = processResults;
        }

        /// <summary>Wraps a failed process result with a message describing which git command failed.</summary>
        public GitProcessException(ProcessResults processResults, string message) : base(message)
        {
            ProcessResults = processResults;
        }

        /// <summary>Wraps a failed process result together with the exception that triggered it.</summary>
        public GitProcessException(ProcessResults processResults, string message, Exception innerException) : base(message, innerException)
        {
            ProcessResults = processResults;
        }

        /// <summary>Deserialization constructor required by <see cref="Exception"/>'s serialization contract.</summary>
        public GitProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
