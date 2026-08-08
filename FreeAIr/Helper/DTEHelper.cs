using EnvDTE;
using EnvDTE80;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Small helpers over EnvDTE for reading the state of Visual Studio's Solution Explorer, used to
    /// decide whether a command applies to the whole solution or to a specific project.
    /// </summary>
    public static class DTEHelper
    {
        /// <summary>
        /// Whether Solution Explorer has exactly one item selected and it is the solution node
        /// itself, as opposed to a project or file.
        /// </summary>
        public static bool CheckIfOnlySolutionSelected(
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

            UIHierarchy solutionExplorer = dte.ToolWindows.SolutionExplorer;
            UIHierarchyItem[] selectedItems = (UIHierarchyItem[])solutionExplorer.SelectedItems;

            if (selectedItems.Length != 1)
            {
                return false;
            }

            UIHierarchyItem item = selectedItems[0];
            var solutionItem = item.Object as EnvDTE.Solution;
            if (solutionItem is null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Looks up a top-level project of the solution by name (case-insensitive), or null when no
        /// project matches.
        /// </summary>
        public static EnvDTE.Project? TryFindProject(
            this EnvDTE.DTE dte,
            string projectName
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solution = dte.Solution;

            for (var i = 1; i < solution.Projects.Count; i++)
            {
                var project = solution.Projects.Item(i);
                if (StringComparer.InvariantCultureIgnoreCase.Equals(project.Name, projectName))
                {
                    return project;
                }
            }

            return null;
        }
    }
}
