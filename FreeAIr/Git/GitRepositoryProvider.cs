using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using System.IO;
using System.Threading.Tasks;

namespace FreeAIr.Git
{
    public static class GitRepositoryProvider
    {
        public static async Task<bool> IsGitRepositoryExistsAsync()
        {
            var gitExt = (IGitExt)await FreeAIrPackage.Instance.GetServiceAsync(typeof(IGitExt));
            return gitExt is not null && gitExt.ActiveRepositories.Count > 0;
        }

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
