using FreeAIr.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;

namespace FreeAIr.Chat.Context.Composer
{
    /// <summary>
    /// Collects every type a piece of C# depends on, so that the files declaring those types can be
    /// attached to the chat along with the file the user actually asked about.
    ///
    /// This is what makes a question about one method answerable: the model is shown the entities,
    /// the interfaces and the helpers the code touches, without the user naming them one by one.
    /// The result feeds <see cref="CSharpContextComposer"/>, which turns the symbols back into
    /// <see cref="FreeAIr.Chat.Context.Item.SolutionItemChatContextItem"/>s.
    ///
    /// Types from referenced assemblies are dropped on purpose — see <see cref="AddIfLocalType"/>.
    /// Cancellation is checked in every override because a walk over a large file with a semantic
    /// model behind it is slow enough for the user to give up on it.
    /// </summary>
    public sealed class TypeReferenceWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// The types found so far. A set with the symbol comparer, because the same type is reached
        /// again and again along different syntax paths.
        /// </summary>
        private readonly HashSet<ITypeSymbol> _typeSymbols = new(SymbolEqualityComparer.Default);

        /// <summary>Everything the walk turned up. Meaningful only after the walk has finished.</summary>
        public IReadOnlyCollection<ITypeSymbol> ReferencedTypes => _typeSymbols;

        /// <summary>
        /// Walks at node depth: tokens and trivia carry no type information, and skipping them makes
        /// the traversal noticeably cheaper on a big file.
        /// </summary>
        public TypeReferenceWalker(
            SemanticModel semanticModel,
            CancellationToken cancellationToken
            )
            : base(SyntaxWalkerDepth.Node)
        {
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Picks up the type a declaration belongs to before descending into it. This is what makes
        /// a walk over a single member still yield the type declaring it, which the specialized
        /// overrides below would never see.
        /// </summary>
        public override void Visit(SyntaxNode? node)
        {
            if (node is null)
            {
                return;
            }

            var symbol = _semanticModel.GetDeclaredSymbol(node);
            var upperType = symbol.UpToUpper<ITypeSymbol>();
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }
            if (upperType is not null)
            {
                AddIfLocalType(upperType);
            }

            base.Visit(node);
        }

        /// <summary>
        /// Keeps a type only if it is declared in the solution. Having declaring syntax is the test:
        /// a type from a referenced assembly has none, and there would be no source file to attach
        /// to the chat even if it were kept.
        /// </summary>
        private void AddIfLocalType(ITypeSymbol? type)
        {
            if (type is null)
            {
                return;
            }
            if (type.DeclaringSyntaxReferences != null && type.DeclaringSyntaxReferences.Length > 0)
            {
                _typeSymbols.Add(type);
            }
        }

        #region visit methods

        /// <summary>Takes the type from a `new` expression — the most direct statement that this code depends on that type.</summary>
        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node);
            AddIfLocalType(typeInfo.Type);
            base.VisitObjectCreationExpression(node);
        }

        /// <summary>Takes the declared type of a local or a field. `var` resolves through the semantic model, so an inferred type is caught as well.</summary>
        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node.Type);
            AddIfLocalType(typeInfo.Type);
            base.VisitVariableDeclaration(node);
        }

        /// <summary>Takes the type declaring the method being called, which is how static helpers and extension methods pull their own file in.</summary>
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var symbolInfo = _semanticModel.GetSymbolInfo(node);
            if (symbolInfo.Symbol is IMethodSymbol method && method.ContainingType != null)
            {
                AddIfLocalType(method.ContainingType);
            }
            base.VisitInvocationExpression(node);
        }

        /// <summary>Takes the target type of a cast.</summary>
        public override void VisitCastExpression(CastExpressionSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node.Type);
            AddIfLocalType(typeInfo.Type);
            base.VisitCastExpression(node);
        }

        /// <summary>Takes a constructed generic such as `List&lt;Station&gt;`; the argument types are reached separately as identifiers.</summary>
        public override void VisitGenericName(GenericNameSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var symbolInfo = _semanticModel.GetSymbolInfo(node);
            if (symbolInfo.Symbol is ITypeSymbol type)
            {
                AddIfLocalType(type);
            }
            base.VisitGenericName(node);
        }

        /// <summary>Takes the element type of an array creation.</summary>
        public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node.Type);
            AddIfLocalType(typeInfo.Type);
            base.VisitArrayCreationExpression(node);
        }

        /// <summary>Takes any bare name which turns out to denote a type — static member access, a nameof, a type used as an argument.</summary>
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var symbolInfo = _semanticModel.GetSymbolInfo(node);
            if (symbolInfo.Symbol is ITypeSymbol type)
            {
                AddIfLocalType(type);
            }
            base.VisitIdentifierName(node);
        }

        //public override void VisitCollectionInitializer(CollectionInitializerSyntax node)
        //{
        //    if (_cancellationToken.IsCancellationRequested)
        //    {
        //        return;
        //    }

        //    foreach (var expression in node.Initializers)
        //    {
        //        var typeInfo = _semanticModel.GetTypeInfo(expression);
        //        AddIfLocalType(typeInfo.Type);
        //    }
        //    base.VisitCollectionInitializer(node);
        //}

        /// <summary>Takes the operand of a `typeof`, which reflection based code often mentions nowhere else.</summary>
        public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node.Type);
            AddIfLocalType(typeInfo.Type);
            base.VisitTypeOfExpression(node);
        }

        /// <summary>Takes the type of a declaration pattern, so the types tested for in `is T t` are attached too.</summary>
        public override void VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (node.Pattern is DeclarationPatternSyntax declarationPattern)
            {
                var typeInfo = _semanticModel.GetTypeInfo(declarationPattern.Type);
                AddIfLocalType(typeInfo.Type);
            }
            base.VisitIsPatternExpression(node);
        }

        /// <summary>Takes the keyword types such as `int` or `string`. They are always filtered out afterwards, having no declaring syntax in the solution.</summary>
        public override void VisitPredefinedType(PredefinedTypeSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node);
            AddIfLocalType(typeInfo.Type);
            base.VisitPredefinedType(node);
        }

        /// <summary>Takes the synthesized type of an anonymous object. Kept for symmetry; it is never local and so never survives the filter.</summary>
        public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node);
            AddIfLocalType(typeInfo.Type);
            base.VisitAnonymousObjectCreationExpression(node);
        }

        /// <summary>Takes the type a lambda body evaluates to, which is the only place the result type of a short lambda appears.</summary>
        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node.Body);
            AddIfLocalType(typeInfo.Type);
            base.VisitSimpleLambdaExpression(node);
        }

        /// <summary>Takes the type of the returned expression, catching a value whose type is written down nowhere in the method.</summary>
        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var typeInfo = _semanticModel.GetTypeInfo(node.Expression);
            AddIfLocalType(typeInfo.Type);
            base.VisitReturnStatement(node);
        }

        #endregion
    }
}
