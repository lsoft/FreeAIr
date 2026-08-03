using FreeAIr.NLOutline.Json;
using System;
using System.Collections.Generic;

namespace FreeAIr.NLOutline.Tree
{
    /// <summary>
    /// Rebuilds the <see cref="OutlineNode"/> tree out of the flat list stored in
    /// `…_embeddings.outlines.json`.
    ///
    /// The shape of the tree used to be a fourth index file of its own. It is not stored any more:
    /// every node carries its <see cref="OutlineItselfJsonObject.Kind"/> and the path of its file,
    /// which is all the structure anybody consumes — the index is asked "which nodes belong to this
    /// file", never "who is the parent of that member". Keeping a file which only repeats what the
    /// other one already says means one more thing to merge, and one more way for the two to
    /// disagree.
    ///
    /// What the reconstruction cannot recover exactly is the nesting inside a file, because the
    /// parent of a member is not written down anywhere. It is guessed from the `Type.Member` naming
    /// the scanners use, and whatever cannot be matched is attached to the file node. Nothing reads
    /// that nesting for meaning: it is only walked over.
    /// </summary>
    public static class OutlineTreeAssembler
    {
        /// <summary>
        /// Returns null when there is no solution node to hang everything on, which means the file
        /// is not an index of a solution at all.
        /// </summary>
        public static OutlineNode? Assemble(
            IReadOnlyList<OutlineItselfJsonObject> outlines,
            IReadOnlyDictionary<Guid, float[]>? vectors = null
            )
        {
            if (outlines is null)
            {
                throw new ArgumentNullException(nameof(outlines));
            }

            OutlineItselfJsonObject? solution = null;
            var projects = new List<OutlineItselfJsonObject>();
            var files = new List<OutlineItselfJsonObject>();
            var types = new List<OutlineItselfJsonObject>();
            var members = new List<OutlineItselfJsonObject>();

            foreach (var outline in outlines)
            {
                switch (outline.Kind)
                {
                    case OutlineKindEnum.Solution:
                        //a second solution node cannot happen through the generator; if it does,
                        //pick by id so that the result does not depend on the order of the file
                        if (solution is null || outline.Id.CompareTo(solution.Id) < 0)
                        {
                            solution = outline;
                        }
                        break;
                    case OutlineKindEnum.Project:
                        projects.Add(outline);
                        break;
                    case OutlineKindEnum.File:
                        files.Add(outline);
                        break;
                    case OutlineKindEnum.ClassOrSimilarEntity:
                        types.Add(outline);
                        break;
                    case OutlineKindEnum.MethodOfClassOrSimilarPart:
                        members.Add(outline);
                        break;
                }
            }

            if (solution is null)
            {
                return null;
            }

            var typesByPath = GroupByPath(types);
            var membersByPath = GroupByPath(members);

            var fileNodes = new List<OutlineNode>(files.Count);
            foreach (var file in files)
            {
                fileNodes.Add(
                    BuildFileNode(file, typesByPath, membersByPath, vectors)
                    );
            }

            var projectNodes = new List<OutlineNode>(projects.Count);
            var fileNodesByProject = new Dictionary<string, List<OutlineNode>>(StringComparer.OrdinalIgnoreCase);
            var orphanFileNodes = new List<OutlineNode>();

            foreach (var fileNode in fileNodes)
            {
                var projectPath = FindOwningProjectPath(projects, fileNode.RelativePath);
                if (projectPath is null)
                {
                    orphanFileNodes.Add(fileNode);
                    continue;
                }

                if (!fileNodesByProject.TryGetValue(projectPath, out var list))
                {
                    list = new List<OutlineNode>();
                    fileNodesByProject.Add(projectPath, list);
                }

                list.Add(fileNode);
            }

            foreach (var project in projects)
            {
                fileNodesByProject.TryGetValue(project.RelativePath, out var children);
                children ??= new List<OutlineNode>();
                children.Sort(OutlineNode.OrderComparison);

                projectNodes.Add(
                    CreateNode(project, children, vectors)
                    );
            }

            //a file whose project is gone still belongs to the solution: dropping it would silently
            //shrink the index, which is exactly the failure the user cannot see
            projectNodes.AddRange(orphanFileNodes);
            projectNodes.Sort(OutlineNode.OrderComparison);

            return CreateNode(solution, projectNodes, vectors);
        }

        private static OutlineNode BuildFileNode(
            OutlineItselfJsonObject file,
            IReadOnlyDictionary<string, List<OutlineItselfJsonObject>> typesByPath,
            IReadOnlyDictionary<string, List<OutlineItselfJsonObject>> membersByPath,
            IReadOnlyDictionary<Guid, float[]>? vectors
            )
        {
            typesByPath.TryGetValue(file.RelativePath, out var types);
            membersByPath.TryGetValue(file.RelativePath, out var members);

            var childrenByType = new Dictionary<string, List<OutlineNode>>(StringComparer.Ordinal);
            var fileChildren = new List<OutlineNode>();

            if (types is not null)
            {
                foreach (var type in types)
                {
                    if (!childrenByType.ContainsKey(type.Target))
                    {
                        childrenByType.Add(type.Target, new List<OutlineNode>());
                    }
                }
            }

            if (members is not null)
            {
                foreach (var member in members)
                {
                    //`Outer.Method` is how both scanners name a member, so the part before the last
                    //dot is the type it was found in
                    var separator = member.Target?.LastIndexOf('.') ?? -1;
                    var owner = separator > 0
                        ? member.Target!.Substring(0, separator)
                        : null
                        ;

                    var memberNode = CreateNode(member, new List<OutlineNode>(), vectors);

                    if (owner is not null && childrenByType.TryGetValue(owner, out var siblings))
                    {
                        siblings.Add(memberNode);
                    }
                    else
                    {
                        fileChildren.Add(memberNode);
                    }
                }
            }

            if (types is not null)
            {
                foreach (var type in types)
                {
                    var children = childrenByType[type.Target];
                    children.Sort(OutlineNode.OrderComparison);

                    fileChildren.Add(
                        CreateNode(type, children, vectors)
                        );
                }
            }

            fileChildren.Sort(OutlineNode.OrderComparison);

            return CreateNode(file, fileChildren, vectors);
        }

        /// <summary>
        /// The project whose folder contains the file. The longest match wins, so that a project
        /// nested inside another one takes its own files.
        /// </summary>
        private static string? FindOwningProjectPath(
            List<OutlineItselfJsonObject> projects,
            string filePath
            )
        {
            string? best = null;

            foreach (var project in projects)
            {
                var folder = GetFolder(project.RelativePath);
                if (folder.Length == 0)
                {
                    //a project in the solution folder itself owns anything nobody else claims
                    best ??= project.RelativePath;
                    continue;
                }

                if (filePath.Length <= folder.Length
                    || !filePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (best is null || folder.Length > GetFolder(best).Length)
                {
                    best = project.RelativePath;
                }
            }

            return best;
        }

        private static string GetFolder(
            string relativePath
            )
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return string.Empty;
            }

            var separator = relativePath.LastIndexOfAny(new[] { '\\', '/' });
            return separator < 0
                ? string.Empty
                : relativePath.Substring(0, separator + 1)
                ;
        }

        private static IReadOnlyDictionary<string, List<OutlineItselfJsonObject>> GroupByPath(
            List<OutlineItselfJsonObject> outlines
            )
        {
            var result = new Dictionary<string, List<OutlineItselfJsonObject>>(StringComparer.OrdinalIgnoreCase);

            foreach (var outline in outlines)
            {
                var path = outline.RelativePath ?? string.Empty;

                if (!result.TryGetValue(path, out var list))
                {
                    list = new List<OutlineItselfJsonObject>();
                    result.Add(path, list);
                }

                list.Add(outline);
            }

            return result;
        }

        private static OutlineNode CreateNode(
            OutlineItselfJsonObject outline,
            List<OutlineNode> children,
            IReadOnlyDictionary<Guid, float[]>? vectors
            )
        {
            float[]? vector = null;
            vectors?.TryGetValue(outline.Id, out vector);

            return new OutlineNode(
                outline.Id,
                outline.Kind,
                outline.RelativePath ?? string.Empty,
                outline.Target ?? string.Empty,
                outline.OutlineText ?? string.Empty,
                vector,
                children
                );
        }
    }
}
