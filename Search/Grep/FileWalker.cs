using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FreeAIr.Grep
{
    /// <summary>
    /// Walks a source tree the way a developer thinks of it: the folders which hold build output,
    /// version control internals and restored packages are not part of it.
    ///
    /// Without that list a search over a solution folder spends its whole budget inside `bin`,
    /// `obj` and `.git`, and answers with the copies rather than with the sources.
    /// </summary>
    public static class FileWalker
    {
        /// <summary>
        /// Folder names which are skipped whole. Matched by name at any depth and without regard to
        /// case, which is what makes the list short enough to be read.
        /// </summary>
        public static readonly IReadOnlyCollection<string> DefaultExcludedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin",
            "obj",
            ".git",
            ".svn",
            ".hg",
            ".vs",
            ".vscode",
            ".idea",
            ".art",
            "node_modules",
            "packages",
            "TestResults",
            "artifacts"
        };

        /// <summary>
        /// Every file under the root, deepest folders last. Folders which cannot be read are
        /// skipped rather than thrown about: a source tree usually has one, and it is never the one
        /// the search was about.
        /// </summary>
        public static IEnumerable<string> EnumerateFiles(
            string rootFolder,
            CancellationToken cancellationToken
            )
        {
            return EnumerateFiles(
                rootFolder,
                DefaultExcludedFolderNames,
                cancellationToken
                );
        }

        public static IEnumerable<string> EnumerateFiles(
            string rootFolder,
            IReadOnlyCollection<string> excludedFolderNames,
            CancellationToken cancellationToken
            )
        {
            if (string.IsNullOrEmpty(rootFolder))
            {
                throw new ArgumentException($"'{nameof(rootFolder)}' cannot be null or empty.", nameof(rootFolder));
            }

            if (excludedFolderNames is null)
            {
                throw new ArgumentNullException(nameof(excludedFolderNames));
            }

            var excluded = new HashSet<string>(excludedFolderNames, StringComparer.OrdinalIgnoreCase);

            var folders = new Stack<string>();
            folders.Push(rootFolder);

            while (folders.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var folder = folders.Pop();

                string[] files;
                try
                {
                    files = Directory.GetFiles(folder);
                }
                catch (Exception excp) when (excp is UnauthorizedAccessException || excp is IOException)
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                string[] children;
                try
                {
                    children = Directory.GetDirectories(folder);
                }
                catch (Exception excp) when (excp is UnauthorizedAccessException || excp is IOException)
                {
                    continue;
                }

                foreach (var child in children)
                {
                    var name = Path.GetFileName(child);
                    if (excluded.Contains(name))
                    {
                        continue;
                    }

                    folders.Push(child);
                }
            }
        }
    }
}
