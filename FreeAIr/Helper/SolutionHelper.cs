using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace FreeAIr.Helper
{
    public static class SolutionHelper
    {
        public const string DatabaseProjectKind = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
        public const string CSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";


        public static async Task<List<SolutionItem>> ProcessDownRecursivelyForAsync(
            this SolutionItem item,
            SolutionItemType type,
            string? fullPath
            )
        {
            var result = new List<SolutionItem>();

            //https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/issues/401
            item.GetItemInfo(out IVsHierarchy hierarchy, out uint itemID, out _);
            if (HierarchyUtilities.TryGetHierarchyProperty(hierarchy, itemID, (int)__VSHPROPID.VSHPROPID_IsNonMemberItem, out bool isNonMemberItem))
            {
                if (isNonMemberItem)
                {
                    // The item is not usually visible. Skip it.
                    return result;
                }
            }

            if (item.Type == type && (string.IsNullOrEmpty(fullPath) || fullPath == item.FullPath))
            {
                result.Add(item);
            }

            foreach (var child in item.Children)
            {
                if (child == null)
                {
                    continue;
                }

                result.AddRange(await child.ProcessDownRecursivelyForAsync(type, fullPath));
            }

            return result;
        }

        public static async Task<List<string>> GetAllFilesFromAsync(
            )
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();

            if (solution == null)
            {
                return new List<string>();
            }

            var files = await solution.ProcessDownRecursivelyForAsync(SolutionItemType.PhysicalFile, null);
            return files.ConvertAll(i => i.FullPath!).FindAll(i => !string.IsNullOrEmpty(i));
        }


        public static async Task<ProjectItemInformation?> TryGetProjectItemAsync(
            string filePath
            )
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution != null)
            {
                var projects = await solution.ProcessDownRecursivelyForAsync(SolutionItemType.Project, null);
                var result = await TryGetProjectItemAsync(projects, filePath);
                return result;
            }

            return null;
        }

        public static async Task<ProjectItemInformation?> TryGetProjectItemAsync(
            List<SolutionItem> projects,
            string filePath
            )
        {
            foreach (var project in projects)
            {
                var files = await project.ProcessDownRecursivelyForAsync(SolutionItemType.PhysicalFile, filePath);
                if (files.Count > 0)
                {
                    return new ProjectItemInformation(project, files[0]);
                }
            }

            return null;
        }

        public static async Task<Dictionary<string, ProjectItemInformation>> GetAllProjectItemsAsync(
            List<SolutionItem>? projects
            )
        {
            var result = new Dictionary<string, ProjectItemInformation>();

            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution == null)
            {
                return result;
            }

            if(projects == null)
            {
                projects = await solution.ProcessDownRecursivelyForAsync(SolutionItemType.Project, null);
            }

            foreach (var project in projects!)
            {
                var files = await project.ProcessDownRecursivelyForAsync(SolutionItemType.PhysicalFile, null);
                foreach(var file in files)
                {
                    if(string.IsNullOrEmpty(file.FullPath))
                    {
                        continue;
                    }

                    if(result.ContainsKey(file.FullPath!))
                    {
                        continue;
                    }

                    result[file.FullPath!] = new ProjectItemInformation(project, file);
                }
            }

            return result;
        }

    }

    public readonly struct ProjectItemInformation
    {
        public readonly SolutionItem Project;
        public readonly SolutionItem ProjectItem;

        public ProjectItemInformation(
            SolutionItem project,
            SolutionItem projectItem
            )
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (projectItem is null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            Project = project;
            ProjectItem = projectItem;
        }
    }
}
