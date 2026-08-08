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
        /// <summary>Reflected internal map from Roslyn project id to the VS project GUID, used to disambiguate multi-targeted projects.</summary>
        private static readonly FieldInfo _projectToGuidMapField = typeof(VisualStudioWorkspace).Assembly
            .GetType(
                "Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.VisualStudioWorkspaceImpl",
                throwOnError: true)
            .GetField("_projectToGuidMap", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>Reflected internal <see cref="Workspace"/> method resolving a document id to the one active in the current project context.</summary>
        private static readonly MethodInfo _getDocumentIdInCurrentContextMethod = typeof(Workspace).GetMethod(
            "GetDocumentIdInCurrentContext",
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(DocumentId) },
            modifiers: null);


        /// <summary>
        /// Opens the Roslyn document at <paramref name="filePath"/> in the given workspace and
        /// wraps it in a <see cref="DocumentEditor"/> for making code edits, or <c>null</c> if
        /// the file is not part of the workspace.
        /// </summary>
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

        /// <summary>
        /// Lists the file paths of every document in the workspace whose project and document
        /// both satisfy the given predicates, e.g. to gather candidate files for indexing or search.
        /// </summary>
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

        /// <summary>
        /// Looks up the document at <paramref name="filePath"/> together with its parsed syntax
        /// root, returning <c>(null, null)</c> when the file is not in the workspace or has no tree.
        /// </summary>
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

        /// <summary>
        /// Finds the workspace document at <paramref name="filePath"/>, resolving it to the id
        /// active in the current project context when multiple target frameworks produce several
        /// candidate ids for the same file.
        /// </summary>
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
        /// <summary>
        /// Finds the workspace document at <paramref name="filePath"/> that belongs to the project
        /// with the given VS project GUID, for disambiguating files shared by multiple projects.
        /// </summary>
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

        /// <summary>
        /// Resolves a document id to the id of the same document in whichever project is currently
        /// the active context, via the internal <see cref="Workspace"/> API.
        /// </summary>
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
