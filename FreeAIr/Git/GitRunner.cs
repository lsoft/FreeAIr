using FreeAIr.Helper;
using RunProcessAsTask;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Git
{
    public sealed class GitRunner
    {
        public static async Task<ProcessResults> DiffAsync(
            CancellationToken cancellationToken = default
            )
        {
            var result = await ProcessHelper.RunSilentlyAsync(
                await GitRepositoryProvider.GetRepositoryFolderAsync(),
                "git.exe",
                @"diff",
                cancellationToken
                );
            if (result.ExitCode != 0)
            {
                throw new GitProcessException(result, "diff fails");
            }
            return result;
        }

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

    public sealed class GitProcessException : Exception
    {
        public ProcessResults ProcessResults
        {
            get;
        }

        public GitProcessException(
            ProcessResults processResults
            )
        {
            ProcessResults = processResults;
        }

        public GitProcessException(ProcessResults processResults, string message) : base(message)
        {
            ProcessResults = processResults;
        }

        public GitProcessException(ProcessResults processResults, string message, Exception innerException) : base(message, innerException)
        {
            ProcessResults = processResults;
        }

        public GitProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
