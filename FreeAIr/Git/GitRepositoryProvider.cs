using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using System.Threading.Tasks;

namespace FreeAIr.Git
{
    public static class GitRepositoryProvider
    {
        public static async Task<string?> GetRepositoryFolderAsync()
        {
            var gitExt = (IGitExt)await FreeAIrPackage.Instance.GetServiceAsync(typeof(IGitExt));
            if (gitExt is null)
            {
                //todo log
                return null;
            }
            if (gitExt.ActiveRepositories.Count != 1)
            {
                //todo log
                return null;
            }

            var activeRepository = gitExt.ActiveRepositories[0] as IGitRepositoryInfo2;
            if (activeRepository.Remotes.Count != 1)
            {
                //todo log
                return null;
            }

            var repositoryFolder = activeRepository.RepositoryPath;
            return repositoryFolder;
        }
    }
}
