using FreeAIr.Agents;
using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree.Builder.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Tree.Builder
{
    public static class TreeBuilder
    {
        public static async Task<EmbeddingContainer> BuildAsync(
            Agent agent,
            CancellationToken cancellationToken
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            var solution = await VS.Solutions.GetCurrentSolutionAsync();

            var result = EmbeddingContainer.CreateFromScratch(
                new OutlineDescriptor(
                    OutlineKindEnum.Solution,
                    solution.FullPath,
                    string.Empty
                    )
                );

            var root = result.GetRootOutlineNode();

            var projects = await solution
                .ProcessDownRecursivelyForAsync(
                    i => i.Type == SolutionItemType.Project,
                    false,
                    cancellationToken
                    );

            foreach (var foundProject in projects
                .OrderBy(i => i.SolutionItem.FullPath.MakeRelativeAgainst(solution.FullPath))
                )
            {
                var project = foundProject.SolutionItem;

                var projectRoot = root.AddChild(
                    OutlineKindEnum.Project,
                    project.FullPath,
                    string.Empty
                    );

                var files = await project.ProcessDownRecursivelyForAsync(
                    item =>
                        !item.IsNonVisibleItem
                        && item.Type == SolutionItemType.PhysicalFile
                        && item.FullPath.GetFileType() == FileTypeEnum.Text
                        ,
                    false,
                    cancellationToken
                    );

                await FileScannerFactory.CreateFileTreeAsync(
                    agent,
                    projectRoot,
                    files
                        .OrderBy(i => i.SolutionItem.FullPath.MakeRelativeAgainst(solution.FullPath))
                        .Select(i => i.SolutionItem)
                        .ToList()
                    );
            }

            return result;
        }
    }
}
