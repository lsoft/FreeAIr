using FreeAIr.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;

namespace FreeAIr.BLogic.Context.Composer
{
    public sealed class TypeReferenceWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;

        private readonly HashSet<ITypeSymbol> _typeSymbols = new(SymbolEqualityComparer.Default);

        public IReadOnlyCollection<ITypeSymbol> ReferencedTypes => _typeSymbols;

        public TypeReferenceWalker(
            SemanticModel semanticModel,
            CancellationToken cancellationToken
            )
            : base(SyntaxWalkerDepth.Node)
        {
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

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
