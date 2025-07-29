using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.LanguageServices;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Taken from  https://github.com/bert2/microscope completely.
    /// Take a look to that repo, it's amazing!
    /// </summary>
    public static class WorkspaceHelper
    {
        private static readonly FieldInfo _projectToGuidMapField = typeof(VisualStudioWorkspace).Assembly
            .GetType(
                "Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.VisualStudioWorkspaceImpl",
                throwOnError: true)
            .GetField("_projectToGuidMap", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo _getDocumentIdInCurrentContextMethod = typeof(Workspace).GetMethod(
            "GetDocumentIdInCurrentContext",
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(DocumentId) },
            modifiers: null);


        public static async Task<DocumentEditor?> CreateDocumentEditorAsync(
            this Workspace workspace,
            string filePath
            )
        {
            if (workspace is null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var document = workspace.GetDocument(filePath);
            if (document == null)
            {
                return null;
            }

            var documentEditor = await DocumentEditor.CreateAsync(document);
            if (documentEditor == null)
            {
                //skip this document
                return null;
            }

            return documentEditor;
        }

        public static IReadOnlyList<string> EnumerateAllDocumentFilePaths(
            this Workspace workspace,
            Func<Microsoft.CodeAnalysis.Project, bool> projectPredicate,
            Func<Document, bool> documentPredicate
            )
        {
            if (workspace is null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (projectPredicate is null)
            {
                throw new ArgumentNullException(nameof(projectPredicate));
            }

            if (documentPredicate is null)
            {
                throw new ArgumentNullException(nameof(documentPredicate));
            }

            var result = new List<string>();

            foreach (var project in workspace.CurrentSolution.Projects)
            {
                if (!projectPredicate(project))
                {
                    continue;
                }

                foreach (var document in project.Documents)
                {
                    if (!documentPredicate(document))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(document.FilePath))
                    {
                        result.Add(document.FilePath!);
                    }
                }
            }

            return result;
        }

        public static async Task<(Document?, SyntaxNode?)> GetDocumentAndSyntaxRootAsync(this Workspace workspace, string filePath)
        {
            var document = workspace.GetDocument(filePath);
            if (document == null)
            {
                //skip this document
                return (null, null);
            }

            var syntaxRoot = await document.GetSyntaxRootAsync();
            if (syntaxRoot == null)
            {
                //skip this document
                return (null, null);
            }

            return (document, syntaxRoot);
        }

        public static Document? GetDocument(this Workspace workspace, string filePath)
        {
            var sln = workspace.CurrentSolution;

            var candidateId = sln
                .GetDocumentIdsWithFilePath(filePath)
                // VS will create multiple `ProjectId`s for projects with multiple target frameworks.
                // We simply take the first one we find.
                .FirstOrDefault()
                ;
            if (candidateId == null)
            {
                return null;
            }

            var currentContextId = workspace.GetDocumentIdInCurrentContext(candidateId);

            return sln.GetDocument(currentContextId);
        }


        // Code adapted from Microsoft.VisualStudio.LanguageServices.CodeLens.CodeLensCallbackListener.TryGetDocument()
        public static Document? GetDocument(this VisualStudioWorkspace workspace, string filePath, Guid projGuid)
        {
            var projectToGuidMap = (ImmutableDictionary<ProjectId, Guid>)_projectToGuidMapField.GetValue(workspace);
            var sln = workspace.CurrentSolution;

            var candidateId = sln
                .GetDocumentIdsWithFilePath(filePath)
                // VS will create multiple `ProjectId`s for projects with multiple target frameworks.
                // We simply take the first one we find.
                .FirstOrDefault(candidateId => projectToGuidMap.GetValueOrDefault(candidateId.ProjectId) == projGuid)
                ;
            if (candidateId == null)
            {
                return null;
            }

            var currentContextId = workspace.GetDocumentIdInCurrentContext(candidateId);

            return sln.GetDocument(currentContextId);
        }

        public static DocumentId? GetDocumentIdInCurrentContext(
            this Workspace workspace,
            DocumentId? documentId
            )
        {
            return
                (DocumentId?)_getDocumentIdInCurrentContextMethod.Invoke(workspace, new[] { documentId });
        }
    }
}
