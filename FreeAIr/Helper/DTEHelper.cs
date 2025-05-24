using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class DTEHelper
    {
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
