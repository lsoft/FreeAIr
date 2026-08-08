using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using System.IO;
using System.Threading.Tasks;

namespace FreeAIr.Git
{
    /// <summary>
    /// Resolves the git repository backing the current solution through Visual Studio's Team
    /// Explorer git extensibility API, used wherever FreeAIr needs the repository root (diff
    /// collection, commit message building).
    /// </summary>
    public static class GitRepositoryProvider
    {
        /// <summary>
        /// Whether Visual Studio currently has at least one active git repository open.
        /// </summary>
        public static async Task<bool> IsGitRepositoryExistsAsync()
        {
            var gitExt = (IGitExt)await FreeAIrPackage.Instance.GetServiceAsync(typeof(IGitExt));
            return gitExt is not null && gitExt.ActiveRepositories.Count > 0;
        }

        /// <summary>
        /// The folder of the single active git repository with exactly one remote, or the solution's
        /// own folder (falling back to the temp folder) when the repository cannot be pinned down
        /// unambiguously.
        /// </summary>
        public static async Task<string?> GetRepositoryFolderAsync()
        {
            var defaultPath = await GetDefaultPathAsync();

            var gitExt = (IGitExt)await FreeAIrPackage.Instance.GetServiceAsync(typeof(IGitExt));
            if (gitExt is null)
            {
                //todo log
                return defaultPath;
            }
            if (gitExt.ActiveRepositories.Count != 1)
            {
                //todo log
                return defaultPath;
            }

            var activeRepository = gitExt.ActiveRepositories[0] as IGitRepositoryInfo2;
            if (activeRepository.Remotes.Count != 1)
            {
                //todo log
                return defaultPath;
            }

            var repositoryFolder = activeRepository.RepositoryPath;
            if (string.IsNullOrEmpty(repositoryFolder))
            {
                return defaultPath;
            }

            return repositoryFolder;
        }

        /// <summary>
        /// The fallback repository folder used when the git extensibility API cannot resolve one
        /// unambiguously: the current solution's directory, or the system temp folder when there is
        /// no solution.
        /// </summary>
        private static async Task<string> GetDefaultPathAsync()
        {
            var defaultPath = System.IO.Path.GetTempPath();

            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is not null)
            {
                defaultPath = new FileInfo(solution.FullPath).Directory.FullName;
            }

            return defaultPath;
        }
    }
}
