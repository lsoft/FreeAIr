using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Roslyn workspace helpers for looking up a <see cref="Document"/> inside a
    /// <see cref="Microsoft.CodeAnalysis.Project"/>, including source-generated documents that are
    /// not returned by the regular document collection.
    /// </summary>
    public static class RoslynDocumentHelper
    {
        /// <summary>
        /// Finds the <see cref="Document"/> with the given id in the project, falling back to the
        /// project's source-generated documents when it is not among the ordinary source files.
        /// </summary>
        public static async Task<Document> GetDocumentByDocumentIdAsync(
            this Microsoft.CodeAnalysis.Project project,
            DocumentId documentId
            )
        {
            var document = project.GetDocument(
                documentId
            );
            if (document != null)
            {
                return document;
            }

            document = (await project.GetSourceGeneratedDocumentsAsync(CancellationToken.None))
                .First(d => d.Id.Equals(documentId));
            return document;
        }
    }
}
