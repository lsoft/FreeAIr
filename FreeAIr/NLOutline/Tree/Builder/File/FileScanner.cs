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
using System.Text;
using System.Threading.Tasks;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.NLOutline.Tree.Builder.File
{
    /// <summary>
    /// Contract for a scanner that turns the files of a solution into <see cref="OutlineNode"/>
    /// entries for the natural language outline tree. Implementations are MEF-exported and picked
    /// per file by extension, or as the LLM-driven fallback for anything unrecognized.
    /// </summary>
    public interface IFileScanner
    {
        /// <summary>
        /// Selection priority among the registered scanners: lower runs first, and
        /// <see cref="int.MaxValue"/> marks the LLM-driven fallback that only matches when nothing
        /// more specific claims the file.
        /// </summary>
        int Order
        {
            get;
        }

        /// <summary>
        /// Short display name for this scanner, shown wherever the available NLO scanners are listed.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// One-line explanation of what this scanner does and how it produces outlines.
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        /// File extensions (including the leading dot) this scanner handles; an empty list means it
        /// matches any file not claimed by a more specific scanner.
        /// </summary>
        IReadOnlyList<string> FileExtensions
        {
            get;
        }

        /// <summary>
        /// Scans <paramref name="items"/> and appends the resulting outline nodes as children of
        /// <paramref name="root"/>.
        /// </summary>
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
        /// <summary>
        /// Runs last among the registered scanners, so it only picks up files no other scanner
        /// claimed by extension.
        /// </summary>
        public int Order => int.MaxValue;

        /// <summary>
        /// Display name for this fallback scanner.
        /// </summary>
        public string Name => "GetDefaultAsync file scanner";

        /// <summary>
        /// Explains that this scanner asks the LLM to produce the NLO tree for a file, rather than
        /// parsing it itself.
        /// </summary>
        public string Description => "GetDefaultAsync scanner for NLO. It asks LLM to produce NLO tree for the file.";

        /// <summary>
        /// Always empty: this is the catch-all scanner, so it matches any file extension not
        /// claimed by a more specific <see cref="IFileScanner"/>.
        /// </summary>
        public IReadOnlyList<string> FileExtensions
        {
            get;
        }

        /// <summary>Creates the fallback scanner with an empty extension list, so it matches whatever no other <see cref="IFileScanner"/> claims.</summary>
        public FileScanner()
        {
            FileExtensions = new List<string>();
        }

        /// <summary>
        /// Entry point that hands the file list to the LLM-based outline generation and folds the
        /// results into <paramref name="root"/>.
        /// </summary>
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

        /// <summary>
        /// Starts a throwaway chat with the configured NLO agent, feeds it each file in turn as
        /// context, and stores the LLM's outline reply as a file-level node under
        /// <paramref name="root"/>. The chat prompt is archived and the context item removed after
        /// each file so the next file starts from a clean slate.
        /// </summary>
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

        /// <summary>Builds the outline prompt for one file from the support action's template, sends it to the already-open chat, and waits for the model's cleaned-up answer.</summary>
        private static async Task<string> ProcessOutlinePromptAsync(
            SupportActionJson action,
            FreeAIr.Chat.Chat chat,
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
        /// <summary>Runs before the LLM-driven fallback so any `.cs` file is parsed with Roslyn rather than sent to the model.</summary>
        public int Order => 1000;

        /// <summary>Display name for this scanner.</summary>
        public string Name => "C# code file scanner";

        /// <summary>Explains that this scanner parses C# source with Roslyn to extract the NLO comments already embedded in it, rather than asking the LLM to generate them.</summary>
        public string Description => "Scanner for NLO in C# source code. It uses Roslyn to extract all embedded NLO in the file.";

        /// <summary>Only `.cs` files are handled by this scanner.</summary>
        public IReadOnlyList<string> FileExtensions
        {
            get;
        }

        /// <summary>Creates the scanner restricted to `.cs` files.</summary>
        public CSharpFileScanner()
        {
            FileExtensions = [".cs"];
        }

        /// <summary>Resolves each item's Roslyn <see cref="Microsoft.CodeAnalysis.Document"/> from the Visual Studio workspace and runs a <see cref="FileOutlineTreeBuilder"/> over it, adding the resulting file node to <paramref name="root"/>.</summary>
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

        /// <summary>
        /// Walks one C# document's Roslyn syntax tree and builds its NLO subtree: a file node whose
        /// children are the top-level types/enums/delegates, each carrying its own members. This is
        /// the piece that decides exactly what text from a comment ends up embedded — see the
        /// class-level notes on <see cref="CSharpFileScanner"/> for what gets read.
        /// </summary>
        public sealed class FileOutlineTreeBuilder
        {
            /// <summary>Solution root, used to compute the file's path relative to it for the outline node's path.</summary>
            private readonly string _rootPath;
            /// <summary>The Roslyn document being scanned.</summary>
            private readonly Document _document;

            /// <summary>Creates a builder scoped to one document, resolving relative paths against <paramref name="rootPath"/>.</summary>
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

            /// <summary>Parses the document's syntax tree, finds every top-level (non-nested) type, enum and delegate declaration, and builds the file's outline node from them.</summary>
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

                // Find all top-level declarations (not nested)
                var rootDeclarations = new List<MemberDeclarationSyntax>();

                foreach (var node in rootSyntax.DescendantNodes())
                {
                    if (node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax
                        && node is MemberDeclarationSyntax declaration
                        && !IsNested(declaration)
                        )
                    {
                        rootDeclarations.Add(declaration);
                    }
                }

                foreach (var declaration in rootDeclarations)
                {
                    var declarationNode = ProcessDeclaration(declaration);
                    if (declarationNode is not null)
                    {
                        fileNode.AddChild(declarationNode);
                    }
                }

                return fileNode;
            }

            /// <summary>True when <paramref name="declaration"/> sits inside another type declaration, so it is excluded from the top-level scan and instead visited when its containing type is processed.</summary>
            private bool IsNested(MemberDeclarationSyntax declaration)
            {
                // A nested declaration is inside another type
                return declaration.Parent is TypeDeclarationSyntax;
            }

            /// <summary>
            /// One node for anything a file can declare at namespace level. Enums and delegates get
            /// one too: their kind in the tree is <see cref="OutlineKindEnum.ClassOrSimilarEntity"/>,
            /// same as a class, because the index has no finer kind and every consumer only asks
            /// which file a node belongs to.
            /// </summary>
            private OutlineNode ProcessDeclaration(MemberDeclarationSyntax declaration)
            {
                switch (declaration)
                {
                    case TypeDeclarationSyntax typeDecl:
                        return ProcessTypeDeclaration(typeDecl);
                    case EnumDeclarationSyntax enumDecl:
                        return ProcessEnumDeclaration(enumDecl);
                    case DelegateDeclarationSyntax delegateDecl:
                        return CreateEntityNode(
                            delegateDecl.Identifier.Text,
                            GetOutlineText(delegateDecl, true),
                            []
                            );
                    default:
                        return null;
                }
            }

            /// <summary>Builds the node for a class/struct/interface/record: one child per method/property/field/event/indexer/constructor member, plus a recursive child for each nested type/enum/delegate.</summary>
            private OutlineNode ProcessTypeDeclaration(TypeDeclarationSyntax typeDecl)
            {
                var typeName = typeDecl.Identifier.Text;

                var typeNode = CreateEntityNode(
                    typeName,
                    GetOutlineText(typeDecl, true),
                    []
                    );

                foreach (var member in typeDecl.Members)
                {
                    //BaseMethodDeclarationSyntax covers methods, constructors, destructors and
                    //operators; BasePropertyDeclarationSyntax covers properties, indexers and
                    //property-style events; BaseFieldDeclarationSyntax covers fields and field-style
                    //events. Anything a developer can put a summary on is in one of the three.
                    if (member is not (BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax or BaseFieldDeclarationSyntax))
                        continue;

                    typeNode.AddChild(
                        CreateMemberNode(
                            typeName,
                            GetMemberName(member),
                            GetOutlineText(member, false)
                            )
                        );
                }

                foreach (var nested in typeDecl.Members)
                {
                    if (nested is not (BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
                    {
                        continue;
                    }

                    var nestedNode = ProcessDeclaration(nested);
                    if (nestedNode is not null)
                    {
                        typeNode.AddChild(nestedNode);
                    }
                }

                return typeNode;
            }

            /// <summary>
            /// The enum itself plus one node per constant. The constants are worth their own nodes
            /// for the same reason members are: a comment on a single value is often the only place
            /// its meaning is written down.
            /// </summary>
            private OutlineNode ProcessEnumDeclaration(EnumDeclarationSyntax enumDecl)
            {
                var enumName = enumDecl.Identifier.Text;

                var enumNode = CreateEntityNode(
                    enumName,
                    GetOutlineText(enumDecl, true),
                    []
                    );

                foreach (var member in enumDecl.Members)
                {
                    enumNode.AddChild(
                        CreateMemberNode(
                            enumName,
                            member.Identifier.Text,
                            GetOutlineText(member, false)
                            )
                        );
                }

                return enumNode;
            }

            /// <summary>Builds an <see cref="OutlineKindEnum.ClassOrSimilarEntity"/> node for a type/enum/delegate; falls back to the bare identifier as the outline text when there is no comment, which is what later causes <c>OutlineEmbedder.SelectNodesToEmbed</c> to drop it from the index.</summary>
            private OutlineNode CreateEntityNode(
                string target,
                string commentText,
                List<OutlineNode> children
                )
            {
                return new OutlineNode(
                    OutlineKindEnum.ClassOrSimilarEntity,
                    _document.FilePath.MakeRelativeAgainst(_rootPath),
                    target,
                    string.IsNullOrEmpty(commentText) ? target : commentText,
                    null,
                    children
                    );
            }

            /// <summary>Builds an <see cref="OutlineKindEnum.MethodOfClassOrSimilarPart"/> node for one member, named `Owner.Member`; falls back to that name as the outline text when there is no comment, which is what later causes <c>OutlineEmbedder.SelectNodesToEmbed</c> to drop it from the index.</summary>
            private OutlineNode CreateMemberNode(
                string ownerName,
                string memberName,
                string commentText
                )
            {
                //`Owner.Member` is the naming OutlineTreeAssembler splits on to find the owner of a
                //member when it rebuilds the tree out of the flat index
                var target = $"{ownerName}.{memberName}";

                return new OutlineNode(
                    OutlineKindEnum.MethodOfClassOrSimilarPart,
                    _document.FilePath.MakeRelativeAgainst(_rootPath),
                    target,
                    string.IsNullOrEmpty(commentText) ? target : commentText,
                    null,
                    []
                    );
            }

            /// <summary>Collects the comment text embedded for one declaration: for a type/enum/delegate only its leading trivia (the `&lt;summary&gt;` plus `//` lines directly above it); for a member, every comment in its leading and descendant trivia, i.e. inline `//` comments inside the body too.</summary>
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

            /// <summary>Scans a trivia list for `//`, `/* */` and XML doc comments, cleans each one via <see cref="CleanCommentText"/>, and appends the non-empty results to <paramref name="comments"/>.</summary>
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

            /// <summary>For a plain `//`/`/* */` comment, strips the comment markers; for an XML doc comment, extracts and returns only the `&lt;summary&gt;` content via <see cref="AppendXmlText"/> — every other doc tag (`&lt;remarks&gt;`, `&lt;param&gt;`, `&lt;returns&gt;`, `&lt;example&gt;`) is ignored.</summary>
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
                            var builder = new StringBuilder();
                            AppendXmlText(xmlElement.Content, builder);

                            return NormalizeWhitespace(builder.ToString());
                        }
                    }
                }

                return string.Empty;
            }

            /// <summary>
            /// The prose inside a `summary`, nested markup included.
            ///
            /// Everything here ends up in the embedding of the node, so what is dropped is invisibly
            /// lost: taking only the bare text runs would silently throw away every `list` of items
            /// and turn "reads the OutlineNode tree" into "reads the tree". The names inside `see`
            /// and `paramref` are kept for the same reason — a type name is often the single most
            /// searchable word of the whole sentence.
            /// </summary>
            private static void AppendXmlText(
                IEnumerable<XmlNodeSyntax> nodes,
                StringBuilder builder
                )
            {
                foreach (var node in nodes)
                {
                    switch (node)
                    {
                        case XmlTextSyntax text:
                            builder.Append(text.GetText().ToString());
                            break;

                        case XmlElementSyntax element:
                            //`c`, `para`, `list`, `item`, `see` with a body — the tag itself carries
                            //no meaning for a search, its content does
                            AppendXmlText(element.Content, builder);

                            //an `item` ends a thought; without this the bullets of a list run into
                            //one another as a single sentence
                            builder.Append(' ');
                            break;

                        case XmlEmptyElementSyntax empty:
                            AppendCrefOrName(empty, builder);
                            break;
                    }
                }
            }

            /// <summary>
            /// The identifier an empty tag points at: `see cref`, `paramref name`, `typeparamref
            /// name`. The `T:`/`M:` prefix a cref may carry is cut off, and so is the namespace —
            /// what is left is the name as it appears in the code, which is what a query would use.
            /// </summary>
            private static void AppendCrefOrName(
                XmlEmptyElementSyntax element,
                StringBuilder builder
                )
            {
                foreach (var attribute in element.Attributes)
                {
                    string value;

                    switch (attribute)
                    {
                        case XmlCrefAttributeSyntax cref:
                            value = cref.Cref.ToString();
                            break;
                        case XmlNameAttributeSyntax name:
                            value = name.Identifier.ToString();
                            break;
                        default:
                            continue;
                    }

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    var colon = value.IndexOf(':');
                    if (colon >= 0 && colon + 1 < value.Length)
                    {
                        value = value.Substring(colon + 1);
                    }

                    builder.Append(' ');
                    builder.Append(value);
                    builder.Append(' ');
                }
            }

            /// <summary>
            /// Collapses the line breaks and the leading slashes of the comment syntax into single
            /// spaces, so that a summary written across several lines embeds the same as the one
            /// sentence it is.
            /// </summary>
            private static string NormalizeWhitespace(
                string text
                )
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return string.Empty;
                }

                var parts = text.Split('\r', '\n');
                var builder = new StringBuilder(text.Length);

                foreach (var part in parts)
                {
                    var trimmed = part.Trim(' ', '\t', '/');
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(trimmed);
                }

                //nested markup leaves double spaces behind where a tag used to be
                var result = builder.ToString();
                while (result.IndexOf("  ", StringComparison.Ordinal) >= 0)
                {
                    result = result.Replace("  ", " ");
                }

                return result.Trim();
            }

            /// <summary>Derives the display name used in a member's `Owner.Member` outline target: the declared identifier for most member kinds, and a fixed placeholder (`constructor`, `destructor`, `this[]`, `conversion`) for the kinds that have none.</summary>
            private string GetMemberName(MemberDeclarationSyntax member)
            {
                return member switch
                {
                    PropertyDeclarationSyntax property => property.Identifier.Text,
                    BaseFieldDeclarationSyntax field when field.Declaration.Variables.Count > 0 =>
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


    /// <summary>
    /// MEF-imports every registered <see cref="IFileScanner"/> and dispatches a batch of solution
    /// files to the right one by extension (or to the LLM-driven fallback), reusing outline nodes
    /// from a previous run for files whose checkbox in the NLO tree UI is unchecked.
    /// </summary>
    [Export(typeof(FileOutlineTreeProcessor))]
    public sealed class FileOutlineTreeProcessor
    {
        /// <summary>Every MEF-registered <see cref="IFileScanner"/>, in import order; sorted by <see cref="IFileScanner.Order"/> when splitting files across them.</summary>
        private readonly IFileScanner[] _scanners;

        /// <summary>Creates the processor with all MEF-imported file scanners.</summary>
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


        /// <summary>Splits <paramref name="fileItems"/> across the registered scanners by extension, reuses each unchecked file's old outline node from <paramref name="parameters"/> where available, and runs the remaining files through their scanner, adding every result under <paramref name="newOutlineRoot"/>.</summary>
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

        /// <summary>Assigns each file to the first scanner (in <see cref="IFileScanner.Order"/> order) whose <see cref="IFileScanner.FileExtensions"/> matches it, or forces every file onto the LLM-driven fallback when <see cref="TreeBuilderParameters.ForceUseNLOAgent"/> is set.</summary>
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
