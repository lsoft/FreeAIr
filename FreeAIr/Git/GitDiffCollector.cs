using FreeAIr.Helper;
using RunProcessAsTask;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Git
{
    /// <summary>
    /// Collects the pending changes of the current git repository — both tracked modifications and
    /// untracked files — as unified diff text, for feeding into an AI commit message prompt.
    /// </summary>
    public static class GitDiffCollector
    {
        /// <summary>
        /// Collects every pending diff and concatenates them into one block of text. Returns null
        /// when there is nothing to diff.
        /// </summary>
        public static async Task<string?> CombineDiffAsync(
            CancellationToken cancellationToken = default
            )
        {
            var summaryDiff = new StringBuilder();

            var diffs = await CollectDiffAsync(cancellationToken);
            if (diffs is null || diffs.Count == 0)
            {
                return null;
            }

            foreach (var diff in diffs)
            {
                summaryDiff.AppendLine(diff);
            }

            return summaryDiff.ToString();
        }

        /// <summary>
        /// Diffs the index against HEAD and diffs every untracked file individually against an empty
        /// file, so newly added files show up in the commit message prompt too. Returns null when
        /// the index diff itself fails or is empty.
        /// </summary>
        public static async Task<List<string>?> CollectDiffAsync(
            CancellationToken cancellationToken = default
            )
        {
            var result = new List<string>();

            var diffIndex = await RunAndParseGitDiffSilentlyAsync(
                string.Empty,
                cancellationToken
                );
            if (string.IsNullOrEmpty(diffIndex))
            {
                //todo log
                return null;
            }
            result.Add(diffIndex);

            var lsFiles = await GitRunner.ListFilesAsync();

            foreach (var lsFile in lsFiles.StandardOutput)
            {
                var diffNoIndex = await RunAndParseGitDiffSilentlyAsync(
                    lsFile,
                    cancellationToken
                    );

                result.Add(diffNoIndex);
            }

            return result;
        }

        /// <summary>
        /// Runs `git diff` for either the tracked index (when <paramref name="nonVersionedFile"/> is
        /// empty) or one untracked file, swallowing any failure so one bad file cannot abort the
        /// whole commit message build.
        /// </summary>
        private static async Task<string?> RunAndParseGitDiffSilentlyAsync(
            string? nonVersionedFile,
            CancellationToken cancellationToken
            )
        {
            try
            {
                ProcessResults diff;
                if (string.IsNullOrEmpty(nonVersionedFile))
                {
                    diff = await GitRunner.DiffAsync();
                }
                else
                {
                    diff = await GitRunner.DiffNewFileAsync(nonVersionedFile);
                }

                if (diff.ExitCode != 0 && diff.ExitCode != 1)
                {
                    return null;
                }
                if (diff.StandardError.Length > 0)
                {
                    return null;
                }
                if (diff.StandardOutput.Length < 5)
                {
                    return null;
                }

                return string.Join(
                    Environment.NewLine,
                    diff.StandardOutput
                    );
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return null;
        }

    }
}
