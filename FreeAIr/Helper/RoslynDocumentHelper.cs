using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class RoslynDocumentHelper
    {
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
