using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Tree.Builder.File
{
    public interface IFileScanner
    {
        int Order
        {
            get;
        }

        string Name
        {
            get;
        }

        string Description
        {
            get;
        }

        IReadOnlyList<string> FileExtensions
        {
            get;
        }

        Task BuildAsync(
            SupportActionJson action,
            AgentJson agent,
            string rootPath,
            List<SolutionItem> items,
            OutlineNode root
            );
    }

    /// <summary>
    /// Default file outline tree node scanner. Used for unknown language (or file type).
    /// LLM produces outlines actually.
    /// </summary>
    [Export(typeof(IFileScanner))]
    public sealed class FileScanner : IFileScanner
    {
        public int Order => int.MaxValue;
        
        public string Name => "GetDefaultAsync file scanner";

        public string Description => "GetDefaultAsync scanner for NLO. It asks LLM to produce NLO tree for the file.";

        public IReadOnlyList<string> FileExtensions
        {
            get;
        }

        public FileScanner()
        {
            FileExtensions = new List<string>();
        }

        public async Task BuildAsync(
            SupportActionJson action,
            AgentJson agent,
            string rootPath,
            List<SolutionItem> items,
            OutlineNode root
            )
        {
            await BuildInternalAsync(
                action,
                agent,
                rootPath,
                items,
                root
                );
        }

        private async Task BuildInternalAsync(
            SupportActionJson action,
            AgentJson agent,
            string rootPath,
            List<SolutionItem> items,
            OutlineNode root
            )
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    null
                    ),
                null,
                await ChatOptions.NoToolAutoProcessedTextResponseAsync(agent)
                );

            if (chat is null)
            {
                return;
            }

            foreach (var item in items)
            {
                var contextItem = new SolutionItemChatContextItem(
                    SelectedIdentifier.Create(
                        item.FullPath,
                        null
                        ),
                    false,
                    AddLineNumbersMode.NotRequired
                    );
                chat.ChatContext.AddItem(contextItem);

                var fileOutline = await ProcessOutlinePromptAsync(action, chat, item);
                if (!string.IsNullOrEmpty(fileOutline))
                {
                    var relative = item.FullPath.MakeRelativeAgainst(rootPath);

                    root.AddChild(
                        OutlineKindEnum.File,
                        relative,
                        relative,
                        fileOutline
                        );
                }

                chat.ChatContext.RemoveItem(contextItem);
                chat.ArchiveAllPrompts();
            }
        }

        private static async Task<string> ProcessOutlinePromptAsync(
            SupportActionJson action,
            Chat chat,
            SolutionItem item
            )
        {
            var supportContext = await SupportContext.WithContextItemAsync(item.FullPath);

            var promptText = supportContext.ApplyVariablesToPrompt(
                action.Prompt
                );

            var prompt = UserPrompt.CreateTextBasedPrompt(promptText);

            chat.AddPrompt(prompt);

            var fileOutline = await chat.WaitForPromptCleanAnswerAsync(
                Environment.NewLine
                );

            return fileOutline;
        }
    }

    /// <summary>
    /// C# outline tree node scanner. Uses Roslyn to extract all outlines from source code.
    /// </summary>
    [Export(typeof(IFileScanner))]
    public sealed class CSharpFileScanner : IFileScanner
    {
        public int Order => 1000;

        public string Name => "C# code file scanner";

        public string Description => "Scanner for NLO in C# source code. It uses Roslyn to extract all embedded NLO in the file.";

        public IReadOnlyList<string> FileExtensions
        {
            get;
        }

        public CSharpFileScanner()
        {
            FileExtensions = [".cs"];
        }

        public async Task BuildAsync(
            SupportActionJson action,
            AgentJson agent,
            string rootPath,
            List<SolutionItem> items,
            OutlineNode root
            )
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var componentModel = (IComponentModel)(await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel)))!;
            if (componentModel == null)
            {
                throw new InvalidOperationException("Can't create a component model");
            }

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            if (workspace == null)
            {
                throw new InvalidOperationException("Can't create a workspace");
            }

            foreach (var item in items)
            {
                var document = workspace.GetDocument(item.FullPath);
                if (document is null)
                {
                    continue;
                }

                var tb = new FileOutlineTreeBuilder(
                    rootPath,
                    document
                    );
                var fileRoot = await tb.CreateOutlineTreeAsync();
                root.AddChild(fileRoot);
            }
        }

        public sealed class FileOutlineTreeBuilder
        {
            private readonly string _rootPath;
            private readonly Document _document;

            public FileOutlineTreeBuilder(
                string rootPath,
                Document document
                )
            {
                if (rootPath is null)
                {
                    throw new ArgumentNullException(nameof(rootPath));
                }

                if (document is null)
                {
                    throw new ArgumentNullException(nameof(document));
                }

                _rootPath = rootPath;
                _document = document;
            }

            public async Task<OutlineNode> CreateOutlineTreeAsync()
            {
                var relative = _document.FilePath.MakeRelativeAgainst(_rootPath);

                var rootSyntax = await _document.GetSyntaxRootAsync();
                var fileNode = new OutlineNode(
                    OutlineKindEnum.File,
                    relative,
                    relative,
                    string.Empty,
                    null,
                    []
                    );

                // Find all top-level types (not nested)
                var rootTypes = new List<TypeDeclarationSyntax>();

                foreach (var node in rootSyntax.DescendantNodes())
                {
                    if (node is TypeDeclarationSyntax typeDecl && !IsNestedType(typeDecl))
                    {
                        rootTypes.Add(typeDecl);
                    }
                }

                foreach (var typeDecl in rootTypes)
                {
                    var typeNode = ProcessTypeDeclaration(typeDecl);
                    fileNode.AddChild(typeNode);
                }

                return fileNode;
            }

            private bool IsNestedType(TypeDeclarationSyntax typeDecl)
            {
                // A nested type is inside another type
                return typeDecl.Parent is TypeDeclarationSyntax;
            }

            private OutlineNode ProcessTypeDeclaration(TypeDeclarationSyntax typeDecl)
            {
                var typeName = typeDecl.Identifier.Text;

                var commentText = GetOutlineText(typeDecl, true);
                var outlineText = string.IsNullOrEmpty(commentText) ? typeName : commentText;

                var typeNode = new OutlineNode(
                    OutlineKindEnum.ClassOrSimilarEntity,
                    _document.FilePath.MakeRelativeAgainst(_rootPath),
                    typeName,
                    outlineText,
                    null,
                    []
                    );

                foreach (var member in typeDecl.Members)
                {
                    if (member is not (BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax or ConstructorDeclarationSyntax))
                        continue;

                    var memberName = GetMemberName(member);
                    var memberComment = GetOutlineText(member, false);
                    var typeMemberNames = $"{typeName}.{memberName}";
                    var memberOutlineText = string.IsNullOrEmpty(memberComment)
                        ? typeMemberNames
                        : memberComment
                        ;

                    var memberNode = new OutlineNode(
                        OutlineKindEnum.MethodOfClassOrSimilarPart,
                        _document.FilePath.MakeRelativeAgainst(_rootPath),
                        typeMemberNames,
                        memberOutlineText,
                        null,
                        []
                        );

                    typeNode.AddChild(memberNode);
                }

                foreach (var nestedType in typeDecl.Members.OfType<TypeDeclarationSyntax>())
                {
                    var nestedNode = ProcessTypeDeclaration(nestedType);
                    typeNode.AddChild(nestedNode);
                }

                return typeNode;
            }

            private string GetOutlineText(
                SyntaxNode node,
                bool onlyXmlComments
                )
            {
                var comments = new List<string>();

                if (onlyXmlComments)
                {
                    ExtractComments(
                        node.GetLeadingTrivia(),
                        ref comments
                        );
                }
                else
                {
                    ExtractComments(
                        node.DescendantTrivia(),
                        ref comments
                        );
                }

                return string.Join(Environment.NewLine, comments);
            }

            private static void ExtractComments(
                IEnumerable<SyntaxTrivia> triviaList,
                ref List<string> comments
                )
            {
                foreach (var trivia in triviaList)
                {
                    if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                        || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                        || trivia.GetStructure() is DocumentationCommentTriviaSyntax
                        )
                    {
                        var cleanedText = CleanCommentText(
                            trivia,
                            trivia.Kind()
                            );
                        if (!string.IsNullOrEmpty(cleanedText))
                        {
                            comments.Add(cleanedText);
                        }
                    }
                }
            }

            private static string CleanCommentText(
                SyntaxTrivia trivia,
                SyntaxKind kind
                )
            {
                if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    var triviaText = trivia.ToString();

                    return triviaText.Trim('/', ' ', '*');
                }

                if (trivia.GetStructure() is DocumentationCommentTriviaSyntax docComment)
                {
                    foreach (var child in docComment.Content)
                    {
                        if (child is XmlElementSyntax xmlElement &&
                            xmlElement.StartTag?.Name?.ToString() == "summary")
                        {
                            var xmlChildren = xmlElement
                                .ChildNodes()
                                .OfType<XmlTextSyntax>()
                                .ToList();

                            var su = string.Join(
                                " ",
                                xmlChildren.SelectMany(
                                    x => x
                                        .GetText()
                                        .ToString()
                                        .Split('\r', '\n')
                                        .Select(p => p.Trim('\r', '\n', ' ', '\t', '/'))
                                    )
                                ).Trim();

                            return su;
                        }
                    }
                }

                return string.Empty;
            }

            private string GetMemberName(MemberDeclarationSyntax member)
            {
                return member switch
                {
                    PropertyDeclarationSyntax property => property.Identifier.Text,
                    FieldDeclarationSyntax field when field.Declaration.Variables.Count > 0 =>
                        field.Declaration.Variables[0].Identifier.Text,
                    ConstructorDeclarationSyntax => "constructor",
                    DestructorDeclarationSyntax => "destructor",
                    EventDeclarationSyntax @event => @event.Identifier.Text,
                    IndexerDeclarationSyntax => "this[]",
                    OperatorDeclarationSyntax op => op.OperatorToken.Text,
                    ConversionOperatorDeclarationSyntax conv => conv.Type?.ToString() ?? "conversion",
                    MethodDeclarationSyntax method => method.Identifier.Text,
                    _ => "unknown"
                };
            }
        }
    }


    [Export(typeof(FileOutlineTreeProcessor))]
    public sealed class FileOutlineTreeProcessor
    {
        private readonly IFileScanner[] _scanners;

        [ImportingConstructor]
        public FileOutlineTreeProcessor(
            [ImportMany] IFileScanner[] scanners
            )
        {
            if (scanners is null)
            {
                throw new ArgumentNullException(nameof(scanners));
            }

            _scanners = scanners;
        }


        public async Task CreateFileTreesAsync(
            TreeBuilderParameters parameters,
            string rootPath,
            OutlineNode newOutlineRoot,
            List<SolutionItem> fileItems
            )
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (newOutlineRoot is null)
            {
                throw new ArgumentNullException(nameof(newOutlineRoot));
            }

            if (fileItems is null)
            {
                throw new ArgumentNullException(nameof(fileItems));
            }

            var splittedItems = SplitItemsByScanners(
                parameters,
                fileItems
                );

            foreach (var pair in splittedItems)
            {
                var solutionItems = pair.SolutionItems;

                //use the old file node if the current file node is not checked
                if (parameters.OldOutlineRoot is not null)
                {
                    for (var i = solutionItems.Count - 1; i >= 0; i--)
                    {
                        var solutionItem = solutionItems[i];
                        if (parameters.TryGetFileOutlineNode(
                            solutionItem.FullPath.MakeRelativeAgainst(rootPath),
                            out var oldOutlineNode)
                            )
                        {
                            newOutlineRoot.AddChild(
                                oldOutlineNode
                                );
                            solutionItems.RemoveAt(i);
                        }
                    }
                }

                if (solutionItems.Count > 0)
                {
                    await pair.FileScanner.BuildAsync(
                        parameters.Action,
                        parameters.Agent,
                        rootPath,
                        solutionItems,
                        newOutlineRoot
                        );
                }
            }
        }

        private List<(IFileScanner FileScanner, List<SolutionItem> SolutionItems)> SplitItemsByScanners(
            TreeBuilderParameters parameters,
            List<SolutionItem> fileItems
            )
        {
            List<(IFileScanner FileScanner, List<SolutionItem> SolutionItems)> list;

            if (parameters.ForceUseNLOAgent)
            {
                list = _scanners
                    .Where(s => s.Order == int.MaxValue) //take the last, based on LLM
                    .Select(f => (FileScanner: f, SolutionItems: new List<SolutionItem>()))
                    .ToList()
                    ;
            }
            else
            {
                list = _scanners
                    .OrderBy(f => f.Order)
                    .Select(f => (FileScanner: f, SolutionItems: new List<SolutionItem>()))
                    .ToList()
                    ;
            }

            foreach (var item in fileItems)
            {
                var extension = new FileInfo(item.FullPath).Extension;
                foreach (var pair in list)
                {
                    if (pair.FileScanner.FileExtensions.Count == 0
                        || pair.FileScanner.FileExtensions.Contains(extension)
                        )
                    {
                        pair.SolutionItems.Add(item);
                        break;
                    }
                }
            }

            return list;
        }
    }
}
