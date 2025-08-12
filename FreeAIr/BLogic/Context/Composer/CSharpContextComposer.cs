using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic.Context.Composer
{
    /// <summary>
    /// Ищет C# типы, связанные с данным, чтобы LLM было понятнее.
    /// </summary>
    public class CSharpContextComposer
    {
        public static async Task<ContextComposeResult> ComposeFromFilePathAsync(
            string filePath,
            TimeSpan? ntimeout = null
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();
            var timeout = ntimeout.GetValueOrDefault(TimeSpan.FromMilliseconds(unsorted.AutomaticSearchForContextItemsTimeoutMsec));

            var context = new ContextComposeResult(
                );

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(timeout);
                var cancellationToken = cancellationTokenSource.Token;


                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                var workspace = (Workspace)componentModel.GetService<VisualStudioWorkspace>();
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (workspace is null)
                {
                    return context;
                }

                var documentIds = workspace.CurrentSolution.GetDocumentIdsWithFilePath(
                    filePath
                    );
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (documentIds.Length == 0)
                {
                    return context;
                }

                var documentId = documentIds.First();
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (document is null)
                {
                    return context;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (root is null)
                {
                    return context;
                }

                await ScanForContextAsync(
                    context,
                    document,
                    root,
                    cancellationToken
                    );
            }
            catch (OperationCanceledException)
            {
                //this is ok
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return context;
        }

        public static async Task<ContextComposeResult> ComposeFromActiveDocumentAsync(
            TimeSpan? ntimeout = null
            )
        {
            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();
            var timeout = ntimeout.GetValueOrDefault(TimeSpan.FromMilliseconds(unsorted.AutomaticSearchForContextItemsTimeoutMsec));

            var context = new ContextComposeResult(
                );

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(timeout);
                var cancellationToken = cancellationTokenSource.Token;


                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                var workspace = (Workspace)componentModel.GetService<VisualStudioWorkspace>();
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (workspace is null)
                {
                    return context;
                }

                var vsDocumentView = await VS.Documents.GetActiveDocumentViewAsync();
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (vsDocumentView is null)
                {
                    return context;
                }

                var vsDocument = vsDocumentView.Document;
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (vsDocument is null)
                {
                    return context;
                }

                var selection = vsDocumentView.TextView.Selection;
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (selection is null)
                {
                    return context;
                }

                var snapshotSpan = selection.StreamSelectionSpan.SnapshotSpan;
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (snapshotSpan.IsEmpty)
                {
                    return context;
                }

                var textSpan = new Microsoft.CodeAnalysis.Text.TextSpan(
                    snapshotSpan.Start,
                    snapshotSpan.Length
                    );

                var vsDocumentFilePath = vsDocument.FilePath;
                context.AddUserProvidedIdentifier(
                    SelectedIdentifier.Create(
                        vsDocumentFilePath,
                        new SelectedSpan(
                            snapshotSpan.Start,
                            snapshotSpan.Length
                            )
                        )
                    );

                var documentIds = workspace.CurrentSolution.GetDocumentIdsWithFilePath(
                    vsDocumentFilePath
                    );
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (documentIds.Length == 0)
                {
                    return context;
                }

                var documentId = documentIds.First();
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (document is null)
                {
                    return context;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (root is null)
                {
                    return context;
                }

                var foundToken = root.FindToken(textSpan.Start);
                if (cancellationToken.IsCancellationRequested)
                {
                    return context;
                }
                if (foundToken.Span.IsEmpty || foundToken.Parent is null)
                {
                    return context;
                }

                var selectedNode = foundToken.Parent.UpTo(typeof(MethodDeclarationSyntax), typeof(BaseTypeDeclarationSyntax), typeof(NamespaceDeclarationSyntax), typeof(CompilationUnitSyntax))
                    ?? foundToken.Parent
                    ;
                if (selectedNode is null)
                {
                    return context;
                }

                await ScanForContextAsync(
                    context,
                    document,
                    selectedNode,
                    cancellationToken
                    );
            }
            catch (OperationCanceledException)
            {
                //this is ok
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return context;
        }

        private static async Task ScanForContextAsync(
            ContextComposeResult contextResult,
            Microsoft.CodeAnalysis.Document document,
            SyntaxNode startNode,
            CancellationToken cancellationToken
            )
        {
            if (contextResult is null)
            {
                throw new ArgumentNullException(nameof(contextResult));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (startNode is null)
            {
                throw new ArgumentNullException(nameof(startNode));
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            if (semanticModel is null)
            {
                return;
            }

            var walker = new TypeReferenceWalker(semanticModel, cancellationToken);
            walker.Visit(startNode);

            contextResult.AddFilePaths(
                walker.ReferencedTypes.SelectMany(
                    t => t.DeclaringSyntaxReferences.Select(
                        dsr => dsr.SyntaxTree.FilePath
                        )
                    ),
                true
                );
            contextResult.AddTypes(
                walker.ReferencedTypes
                );
        }
    }
}
