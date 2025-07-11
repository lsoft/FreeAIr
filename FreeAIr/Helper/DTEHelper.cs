using EnvDTE;
using EnvDTE80;

namespace FreeAIr.Helper
{
    public static class DTEHelper
    {
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
