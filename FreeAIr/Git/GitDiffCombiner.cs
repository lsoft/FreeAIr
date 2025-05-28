using FreeAIr.BLogic;
using FreeAIr.Helper;
using RunProcessAsTask;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Git
{
    public static class GitDiffCombiner
    {
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
                //todo log
            }

            return null;
        }

    }
}
